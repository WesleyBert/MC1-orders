param([switch]$Rapido)

$ErrorActionPreference = 'Stop'
$qa = $PSScriptRoot
$root = Split-Path $qa -Parent
$reports = Join-Path $qa 'reports'
$appUrl = 'http://localhost:8080'

function Wait-App {
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        try {
            if ((Invoke-WebRequest "$appUrl/health/ready" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { return }
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    throw 'A aplicação não ficou pronta. Veja os logs com: docker compose logs'
}

Write-Host '== 1/4 Subindo o sistema no Docker ==' -ForegroundColor Cyan
Push-Location $root
docker compose up --build -d
if ($LASTEXITCODE -ne 0) { throw 'Falha ao subir o Docker. Verifique se o Docker Desktop está aberto.' }
Pop-Location
Wait-App

if (Test-Path $reports) { Remove-Item $reports -Recurse -Force }
New-Item -ItemType Directory -Path $reports | Out-Null

Write-Host '== 2/4 Cenários de uso na tela (Playwright) ==' -ForegroundColor Cyan
Push-Location (Join-Path $qa 'playwright')
if (-not (Test-Path 'node_modules')) {
    npm install
    npx playwright install chromium
}
$env:QA_BASE_URL = $appUrl
$env:QA_ASSISTIR = if ($Rapido) { '0' } else { '1' }
if (-not $Rapido) { Write-Host 'O navegador vai abrir e mostrar cada cenário em câmera lenta.' -ForegroundColor Yellow }
npx playwright test
$playwrightExit = $LASTEXITCODE
Pop-Location

Write-Host '== 3/4 Teste de carga (k6) ==' -ForegroundColor Cyan
if (-not $Rapido) {
    Write-Host 'Os gráficos ao vivo vão abrir numa janela do navegador e fechar sozinhos no fim do teste.' -ForegroundColor Yellow
    $reportFile = Join-Path $reports 'k6-relatorio.html'
    Start-Process node -WindowStyle Hidden -WorkingDirectory (Join-Path $qa 'playwright') -ArgumentList 'painel-ao-vivo.mjs', "`"$reportFile`""
}
docker run --rm `
    -p 5665:5665 `
    -v "${qa}\k6:/scripts" `
    -v "${reports}:/reports" `
    -e BASE_URL=http://host.docker.internal:8080 `
    -e K6_WEB_DASHBOARD=true `
    -e K6_WEB_DASHBOARD_HOST=0.0.0.0 `
    -e K6_WEB_DASHBOARD_PERIOD=2s `
    -e K6_WEB_DASHBOARD_EXPORT=/reports/k6-relatorio.html `
    grafana/k6 run --summary-export=/reports/k6-resumo.json /scripts/carga.js
$k6Exit = $LASTEXITCODE

Write-Host '== 4/4 Montando a página de evidências ==' -ForegroundColor Cyan
& (Join-Path $qa 'montar-evidencias.ps1') -PlaywrightExit $playwrightExit -K6Exit $k6Exit

if ($playwrightExit -ne 0 -or $k6Exit -ne 0) { exit 1 }
