#!/usr/bin/env bash
# Runs the API locally against the SQLite fallback database.
# No SQL Server installation required - great for a quick first run.
set -euo pipefail

cd "$(dirname "$0")/.."

export ASPNETCORE_ENVIRONMENT=Development
export Database__Provider=Sqlite
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://localhost:5231}"

echo "Starting InventoryPro API with SQLite on ${ASPNETCORE_URLS}"
dotnet run --project src/InventoryPro.Api/InventoryPro.Api.csproj --no-launch-profile
