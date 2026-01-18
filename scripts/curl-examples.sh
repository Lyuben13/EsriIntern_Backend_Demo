#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${1:-http://localhost:5079}"

curl -sS "$BASE_URL/api/stats/health"; echo
curl -sS -X POST "$BASE_URL/api/stats/refresh" -H 'Content-Type: application/json' -d '{}' ; echo
curl -sS "$BASE_URL/api/stats/states"; echo
curl -sS "$BASE_URL/api/stats/states/California"; echo
