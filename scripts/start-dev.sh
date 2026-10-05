#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND_URL="http://127.0.0.1:5188"
FRONTEND_DIR="$ROOT_DIR/frontend"
BACKEND_LOG="${TMPDIR:-/tmp}/warehouse-barcode-backend.log"

cleanup() {
  [[ -n "${BACKEND_PID:-}" ]] && kill "$BACKEND_PID" 2>/dev/null || true
  [[ -n "${FRONTEND_PID:-}" ]] && kill "$FRONTEND_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

echo "Starting ASP.NET Core backend at $BACKEND_URL"
if command -v lsof >/dev/null 2>&1; then
  EXISTING_BACKEND_PIDS="$(lsof -ti tcp:5188 2>/dev/null || true)"
  if [[ -n "$EXISTING_BACKEND_PIDS" ]]; then
    echo "Stopping stale backend process(es) on port 5188: $EXISTING_BACKEND_PIDS"
    kill $EXISTING_BACKEND_PIDS 2>/dev/null || true
    sleep 1
  fi
fi
(
  cd "$ROOT_DIR"
  dotnet run --project WarehouseBarcode --no-restore --urls "$BACKEND_URL"
) >"$BACKEND_LOG" 2>&1 &
BACKEND_PID=$!

ready=0
for _ in $(seq 1 60); do
  if curl -fsS "$BACKEND_URL/api/auth/captcha" >/dev/null 2>&1; then ready=1; break; fi
  sleep 1
done
if [[ "$ready" != 1 ]]; then
  echo "Backend failed to start. Log: $BACKEND_LOG" >&2
  sed -n '1,120p' "$BACKEND_LOG" >&2 || true
  exit 1
fi

echo "Starting React frontend at http://127.0.0.1:5173/react/"
(
  cd "$FRONTEND_DIR"
  npm run dev -- --host 127.0.0.1
) &
FRONTEND_PID=$!

echo "Open http://127.0.0.1:5173/react/"
echo "Press Ctrl+C to stop both services."
wait "$FRONTEND_PID"
