#!/usr/bin/env pwsh
# Post-deploy smoke test: a handful of fast, curl-based checks that prove a freshly deployed
# stack is actually serving traffic and its seeded demo data/auth path work end-to-end. Not a
# substitute for the Playwright suites (Vespera.Client/e2e/), which cover full user journeys —
# this is the fast gate that runs first, right after the stack reports healthy.
#
# Usage:
#   ./tools/smoke-test.ps1
#   ./tools/smoke-test.ps1 -ApiBaseUrl http://localhost:8080 -ClientBaseUrl http://localhost:4200

param(
    [string]$ApiBaseUrl = "http://localhost:8080",
    [string]$ClientBaseUrl = "http://localhost:4200"
)

$ErrorActionPreference = "Stop"
$demoPassword = "Passw0rd!23456"

function Fail {
    param([string]$Message)
    Write-Error $Message
    exit 1
}

Write-Host "==> Checking $ApiBaseUrl/health/ready"
try {
    Invoke-RestMethod -Uri "$ApiBaseUrl/health/ready" -Method Get | Out-Null
} catch {
    Fail "API readiness check failed (database/outbox/storage): $_"
}

Write-Host "==> Resolving DEMO tenant"
$tenantLookup = Invoke-RestMethod -Uri "$ApiBaseUrl/api/v1/tenants/by-code/DEMO" -Method Get
if (-not $tenantLookup.tenantId) { Fail "Could not resolve DEMO tenant id" }

Write-Host "==> Logging in as a seeded demo user (priya.sharma)"
$loginBody = @{
    email    = "priya.sharma@demo.vespera.test"
    password = $demoPassword
    deviceId = [guid]::NewGuid().ToString()
} | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/v1/auth/login" -Method Post -Body $loginBody `
    -ContentType "application/json" -Headers @{ "X-Tenant-Id" = $tenantLookup.tenantId }
if (-not $loginResponse.accessToken) { Fail "Login did not return an access token" }

Write-Host "==> Calling an authenticated endpoint with the issued token"
try {
    Invoke-RestMethod -Uri "$ApiBaseUrl/api/v1/dashboard" -Method Get `
        -Headers @{ Authorization = "Bearer $($loginResponse.accessToken)" } | Out-Null
} catch {
    Fail "Authenticated dashboard request failed: $_"
}

Write-Host "==> Checking the client is serving"
try {
    Invoke-WebRequest -Uri "$ClientBaseUrl/" -Method Get -UseBasicParsing | Out-Null
} catch {
    Fail "Client root did not respond: $_"
}

Write-Host "Smoke test passed: API ready, DEMO tenant resolvable, login works, dashboard reachable, client serving."
