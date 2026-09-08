#!/usr/bin/env bash
# Generates the initial EF Core migration for the DigitalBank schema.
# Run this once after cloning, before your first `dotnet run` / `docker-compose up`.
#
# Prerequisites:
#   - .NET 8 SDK installed
#   - EF Core CLI tools: dotnet tool install --global dotnet-ef
#
# Usage:
#   ./scripts/add-initial-migration.sh

set -euo pipefail

cd "$(dirname "$0")/.."

echo "Adding initial EF Core migration..."
dotnet ef migrations add InitialCreate \
  --project src/DigitalBank.Infrastructure \
  --startup-project src/DigitalBank.Api \
  --output-dir Migrations

echo ""
echo "Migration created. Apply it to your database with:"
echo "  dotnet ef database update --project src/DigitalBank.Infrastructure --startup-project src/DigitalBank.Api"
