#!/usr/bin/env sh
set -e
cd "$(dirname "$0")"

APP_URL="http://localhost:8080"
DOCS_URL="$APP_URL/swagger"

docker compose up --build -d

echo "Aguardando a aplicação ficar pronta..."
attempt=0
until curl -fs "$APP_URL/health/ready" > /dev/null 2>&1; do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 60 ]; then
    echo "A aplicação não respondeu a tempo. Veja os logs com: docker compose logs" >&2
    exit 1
  fi
  sleep 2
done

open_url() {
  if command -v xdg-open > /dev/null 2>&1; then xdg-open "$1" > /dev/null 2>&1 &
  elif command -v open > /dev/null 2>&1; then open "$1"
  fi
}

open_url "$APP_URL"
open_url "$DOCS_URL"

echo ""
echo "Aplicação:    $APP_URL"
echo "Documentação: $DOCS_URL"
echo "Logs:         docker compose logs -f"
echo "Parar:        docker compose down"
