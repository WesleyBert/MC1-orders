$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$appUrl = 'http://localhost:8080'
$docsUrl = "$appUrl/scalar/v1"

docker compose up --build -d
if ($LASTEXITCODE -ne 0) {
    Write-Error 'Falha ao subir os containers. Verifique se o Docker Desktop está aberto.'
}

Write-Host 'Aguardando a aplicação ficar pronta...'
$ready = $false
for ($attempt = 0; $attempt -lt 60 -and -not $ready; $attempt++) {
    try {
        $ready = (Invoke-WebRequest "$appUrl/health/ready" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200
    } catch {
        Start-Sleep -Seconds 2
    }
}

if (-not $ready) {
    Write-Error 'A aplicação não respondeu a tempo. Veja os logs com: docker compose logs'
}

Start-Process $appUrl
Start-Process $docsUrl

Write-Host ''
Write-Host "Aplicação:    $appUrl"
Write-Host "Documentação: $docsUrl"
Write-Host 'Logs:         docker compose logs -f'
Write-Host 'Parar:        docker compose down'
