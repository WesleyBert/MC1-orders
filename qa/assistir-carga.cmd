@echo off
cd /d "%~dp0"
if not exist reports mkdir reports
del /q "reports\k6-relatorio.html" 2>nul
echo Os graficos ao vivo vao abrir numa janela do navegador e fechar sozinhos no fim do teste.
start "" /min node "%~dp0playwright\painel-ao-vivo.mjs" "%~dp0reports\k6-relatorio.html"
docker run --rm -p 5665:5665 -v "%~dp0k6:/scripts" -v "%~dp0reports:/reports" -e BASE_URL=http://host.docker.internal:8080 -e K6_WEB_DASHBOARD=true -e K6_WEB_DASHBOARD_HOST=0.0.0.0 -e K6_WEB_DASHBOARD_PERIOD=2s -e K6_WEB_DASHBOARD_EXPORT=/reports/k6-relatorio.html grafana/k6 run /scripts/carga.js
