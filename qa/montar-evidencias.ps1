param([int]$PlaywrightExit = 0, [int]$K6Exit = 0)

$ErrorActionPreference = 'Stop'
$reports = Join-Path $PSScriptRoot 'reports'
$appUrl = 'http://localhost:8080'

$pw = Get-Content (Join-Path $reports 'playwright-resultado.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$k6 = Get-Content (Join-Path $reports 'k6-resumo.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$m = $k6.metrics

$pwPassed = $pw.stats.expected
$pwFailed = $pw.stats.unexpected + $pw.stats.flaky
$checksPassed = $m.checks.passes
$checksTotal = $m.checks.passes + $m.checks.fails
$requests = $m.http_reqs.count
$p95Read = [math]::Round($m.'http_req_duration{tipo:leitura}'.'p(95)', 1)
$p95Write = [math]::Round($m.'http_req_duration{tipo:escrita}'.'p(95)', 1)
$failedRate = [math]::Round($m.http_req_failed.value * 100, 2)
$conflicts = $m.conflitos_detectados.count
$lost = $m.gravacoes_perdidas.count
$maxUsers = $m.vus_max.max
$now = (Get-Item (Join-Path $reports 'k6-resumo.json')).LastWriteTime.ToString('dd/MM/yyyy HH:mm')

$pwOk = $PlaywrightExit -eq 0
$k6Ok = $K6Exit -eq 0
$allOk = $pwOk -and $k6Ok
$badge = { param($ok) if ($ok) { '<span class="ok">Aprovado</span>' } else { '<span class="fail">Reprovado</span>' } }

$html = @"
<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Evidências de teste · MC1 Orders</title>
<style>
  :root { --bg:#f6f7f9; --card:#fff; --text:#18181b; --muted:#6b7280; --ok:#15803d; --okbg:#dcfce7; --fail:#b91c1c; --failbg:#fee2e2; --border:#e5e7eb; --accent:#eef2ff; }
  * { box-sizing: border-box; }
  body { margin:0; font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif; background:var(--bg); color:var(--text); }
  main { max-width: 960px; margin: 0 auto; padding: 32px 16px 48px; }
  h1 { margin: 0 0 4px; font-size: 28px; }
  h2 { margin: 0 0 12px; font-size: 20px; display:flex; gap:12px; align-items:center; flex-wrap:wrap; }
  h3 { margin: 16px 0 8px; font-size: 15px; }
  p.sub { margin: 0 0 24px; color: var(--muted); }
  section { background:var(--card); border:1px solid var(--border); border-radius:14px; padding:20px; margin-bottom:16px; }
  .grid { display:grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap:12px; margin: 12px 0 16px; }
  .stat { border:1px solid var(--border); border-radius:10px; padding:12px; }
  .stat.hero { background: var(--accent); border-color: #c7d2fe; }
  .stat b { display:block; font-size:24px; }
  .stat span { color: var(--muted); font-size: 13px; }
  table { width:100%; border-collapse: collapse; margin: 4px 0 16px; font-size: 14px; }
  th, td { text-align:left; padding:8px 10px; border-bottom:1px solid var(--border); vertical-align: top; }
  th { color: var(--muted); font-weight: 600; font-size: 13px; }
  td.num { font-weight: 700; white-space: nowrap; }
  .ok, .fail { font-size: 13px; font-weight: 600; padding: 3px 10px; border-radius: 999px; }
  .ok { color: var(--ok); background: var(--okbg); }
  .fail { color: var(--fail); background: var(--failbg); }
  ul { margin: 0 0 16px; padding-left: 20px; }
  li { margin: 4px 0; }
  a.button { display:inline-block; background:#18181b; color:#fff; text-decoration:none; padding:10px 16px; border-radius:10px; font-weight:600; }
  .note { color: var(--muted); font-size: 14px; }
</style>
</head>
<body>
<main>
  <h1>Evidências de teste · MC1 Orders</h1>
  <p class="sub">Executado em $now, contra o sistema rodando no Docker ($appUrl). Resultado geral: $(& $badge $allOk)</p>

  <section>
    <h2>Teste de carga com $maxUsers usuários ao mesmo tempo (k6) $(& $badge $k6Ok)</h2>
    <p>O k6 simulou <b>$maxUsers pessoas usando a API ao mesmo tempo</b>, durante cerca de 1 minuto, divididas em três grupos:</p>
    <table>
      <tr><th>Usuários</th><th>O que cada um ficou fazendo, sem parar</th></tr>
      <tr><td class="num">80</td><td>Navegando: listando, buscando, filtrando por status, ordenando, mudando de página e vendo o resumo.</td></tr>
      <tr><td class="num">20</td><td>Cadastrando: criando um pedido, consultando, editando e depois pagando ou excluindo.</td></tr>
      <tr><td class="num">5</td><td>Testando o conflito: criando um pedido e mandando <b>duas gravações dele no mesmo instante</b>, como se duas pessoas clicassem em "Salvar" juntas.</td></tr>
    </table>

    <h3>Resultado</h3>
    <div class="grid">
      <div class="stat hero"><b>$maxUsers</b><span>usuários ao mesmo tempo</span></div>
      <div class="stat hero"><b>$requests</b><span>requisições feitas em ~1 minuto</span></div>
      <div class="stat"><b>$failedRate%</b><span>requisições com erro inesperado (meta: menos de 1%)</span></div>
      <div class="stat"><b>$p95Read ms</b><span>95% das leituras responderam abaixo disso (meta: 300 ms)</span></div>
      <div class="stat"><b>$p95Write ms</b><span>95% das gravações responderam abaixo disso (meta: 500 ms)</span></div>
      <div class="stat"><b>$checksPassed / $checksTotal</b><span>verificações aprovadas</span></div>
    </div>

    <h3>Duas pessoas salvando o mesmo pedido, dentro desse teste</h3>
    <div class="grid">
      <div class="stat"><b>$conflicts</b><span>vezes em que duas gravações do mesmo pedido chegaram juntas e o sistema deixou só uma passar (a outra recebeu o aviso 412)</span></div>
      <div class="stat"><b>$lost</b><span>vezes em que as duas passaram e uma apagou a outra (meta: zero)</span></div>
    </div>
    <a class="button" href="k6-relatorio.html">Abrir relatório do k6 (gráficos)</a>
  </section>

  <section>
    <h2>Cenários reais de uso na tela (Playwright) $(& $badge $pwOk)</h2>
    <p>Um navegador de verdade usando o sistema como uma pessoa faria, com vídeo e prints de cada passo.</p>
    <div class="grid">
      <div class="stat hero"><b>$pwPassed</b><span>cenários aprovados</span></div>
      <div class="stat"><b>$pwFailed</b><span>cenários reprovados</span></div>
    </div>
    <ul>
      <li>Lista, busca sem acento ("joao" encontra "João"), filtro por status, paginação e filtros mantidos ao recarregar.</li>
      <li>Cadastro com erros de validação em português, valor formatado em reais ao digitar e sucesso.</li>
      <li><b>Duas pessoas editando o mesmo pedido</b>, em dois navegadores abertos ao mesmo tempo: a segunda é avisada e não perde o que digitou (com vídeo das duas telas).</li>
      <li>A lista se atualiza sozinha em até 10 segundos, sem recarregar a página.</li>
      <li>Pedido pago fica somente leitura e não pode ser excluído.</li>
      <li>Exclusão com confirmação.</li>
    </ul>
    <a class="button" href="playwright/index.html">Abrir relatório do Playwright (vídeos e prints)</a>
  </section>

  <p class="note">Estes testes comprovam que o sistema se comporta como esperado em uso real e sob carga. Para produção,
  ainda faltariam os itens listados no README: banco de dados, autenticação e execução em mais de uma instância.</p>
</main>
</body>
</html>
"@

$index = Join-Path $reports 'index.html'
Set-Content -Path $index -Value $html -Encoding UTF8
Write-Host "Evidências salvas em: $index" -ForegroundColor Green
Start-Process $index
