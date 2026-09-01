#!/usr/bin/env pwsh
# Builds a self-contained EF Core migrations bundle — the discrete deployment artifact that
# applies pending Postgres migrations against a target connection string. Production no longer
# migrates automatically at API startup (see DatabaseServiceCollectionExtensions.cs); this bundle
# (or tools/publish-migrations-bundle.sh) is what a deploy pipeline runs instead, before the new
# API image goes live.
#
# Usage:
#   ./tools/publish-migrations-bundle.ps1 [-OutputPath ./artifacts/efbundle]
#
# Applying it:
#   ./artifacts/efbundle --connection "Host=...;Database=...;Username=...;Password=..."

param(
    [string]$OutputPath = "./artifacts/efbundle"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

dotnet tool restore --tool-manifest "$repoRoot/.config/dotnet-tools.json"
if (-not $?) { exit 1 }

dotnet ef migrations bundle `
    --project "$repoRoot/Vespera.Infrastructure/Vespera.Infrastructure.csproj" `
    --startup-project "$repoRoot/Vespera.Api/Vespera.Api.csproj" `
    --configuration Release `
    --self-contained `
    --force `
    --output $OutputPath

if (-not $?) { exit 1 }

Write-Host "Migration bundle written to $OutputPath"
