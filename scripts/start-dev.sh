#!/usr/bin/env bash
set -euo pipefail

cd /app/Bookshelf

npm ci --no-fund --no-audit
npm run css:build
npm run css:watch &
TAILWIND_PID=$!

cleanup() {
  kill "$TAILWIND_PID" 2>/dev/null || true
}

trap cleanup EXIT INT TERM

dotnet watch run --project /app/Bookshelf --urls http://0.0.0.0:8080 --non-interactive
