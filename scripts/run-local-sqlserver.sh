#!/usr/bin/env bash
# Runs the API locally against SQL Server (the default provider).
# Make sure SQL Server is reachable and the connection string is correct.
set -euo pipefail

cd "$(dirname "$0")/.."

export ASPNETCORE_ENVIRONMENT=Development
export Database__Provider=SqlServer
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Server=localhost,1433;Database=InventoryProDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true}"
export Jwt__Key="${Jwt__Key:-dev-only-signing-key-please-change-to-32-plus-chars}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://localhost:5231}"

echo "Starting InventoryPro API with SQL Server on ${ASPNETCORE_URLS}"
dotnet run --project src/InventoryPro.Api/InventoryPro.Api.csproj --no-launch-profile
