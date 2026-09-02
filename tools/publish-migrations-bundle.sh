#!/usr/bin/env bash
# Builds a self-contained EF Core migrations bundle — the discrete deployment artifact that
# applies pending Postgres migrations against a target connection string. Production no longer
# migrates automatically at API startup (see DatabaseServiceCollectionExtensions.cs); this bundle
# (or tools/publish-migrations-bundle.ps1) is what a deploy pipeline runs instead, before the new
# API image goes live.
#
# Usage:
#   ./tools/publish-migrations-bundle.sh [output-path]     # defaults to ./artifacts/efbundle
#
# Applying it:
#   ./artifacts/efbundle --connection "Host=...;Database=...;Username=...;Password=..."
#
# NOTE: with no IDesignTimeDbContextFactory<VesperaDbContext>, `dotnet ef` has to build the whole
# app host (via Vespera.Api's Program.cs) to locate the DbContext, which runs the app's own
# Phase 3 auto-provisioner as a side effect — on a machine with a locally reachable Postgres this
# can create/reuse a throwaway "vespera_dev" database while just building the bundle. Harmless,
# but worth knowing before this runs somewhere unexpected.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output_path="${1:-./artifacts/efbundle}"

dotnet tool restore --tool-manifest "$repo_root/.config/dotnet-tools.json"

dotnet ef migrations bundle \
    --project "$repo_root/Vespera.Infrastructure/Vespera.Infrastructure.csproj" \
    --startup-project "$repo_root/Vespera.Api/Vespera.Api.csproj" \
    --configuration Release \
    --self-contained \
    --force \
    --output "$output_path"

echo "Migration bundle written to $output_path"
