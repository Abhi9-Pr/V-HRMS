#!/usr/bin/env pwsh
# Runs the k6 scripts (k6/) against an already-running docker-compose stack — k6 isn't a
# docker-compose service itself (it runs a test and exits, not a long-running process), just a
# one-shot container attached to the same network. Requires `docker compose up -d` to have been
# run first.
#
# Usage:
#   ./tools/run-load-tests.ps1 seed                          # seed-employees.js, 5000 employees
#   ./tools/run-load-tests.ps1 seed 500                       # seed-employees.js, 500 employees
#   ./tools/run-load-tests.ps1 dashboard
#   ./tools/run-load-tests.ps1 payroll-dry-run
#   ./tools/run-load-tests.ps1 attendance-grid
#   ./tools/run-load-tests.ps1 all                             # seed (5000) then all three load tests

param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet("seed", "dashboard", "payroll-dry-run", "attendance-grid", "all")]
    [string]$Target,

    [Parameter(Position = 1)]
    [int]$SeedCount = 5000
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$networkName = ((Split-Path -Leaf $repoRoot) -replace '[^a-z0-9]', '').ToLowerInvariant() + "_default"

function Invoke-K6Script {
    param([string]$Script, [string[]]$ExtraArgs = @())

    Write-Host "==> Running k6/$Script"
    docker run --rm --network $networkName -v "${repoRoot}/k6:/scripts" -e BASE_URL=http://api:8080 @ExtraArgs `
        grafana/k6 run "/scripts/$Script"
    if (-not $?) { exit 1 }
}

switch ($Target) {
    "seed" { Invoke-K6Script -Script "seed-employees.js" -ExtraArgs @("-e", "EMPLOYEE_COUNT=$SeedCount") }
    "dashboard" { Invoke-K6Script -Script "dashboard.js" }
    "payroll-dry-run" { Invoke-K6Script -Script "payroll-dry-run.js" }
    "attendance-grid" { Invoke-K6Script -Script "attendance-grid.js" }
    "all" {
        Invoke-K6Script -Script "seed-employees.js" -ExtraArgs @("-e", "EMPLOYEE_COUNT=$SeedCount")
        Invoke-K6Script -Script "dashboard.js"
        Invoke-K6Script -Script "payroll-dry-run.js"
        Invoke-K6Script -Script "attendance-grid.js"
    }
}
