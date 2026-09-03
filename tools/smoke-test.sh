#!/usr/bin/env bash
# Post-deploy smoke test: a handful of fast, curl-based checks that prove a freshly deployed
# stack is actually serving traffic and its seeded demo data/auth path work end-to-end. Not a
# substitute for the Playwright suites (Vespera.Client/e2e/), which cover full user journeys —
# this is the fast gate that runs first, right after the stack reports healthy.
#
# Usage:
#   API_BASE_URL=http://localhost:8080 CLIENT_BASE_URL=http://localhost:4200 ./tools/smoke-test.sh
#
# Both env vars default to the docker-compose.yml + docker-compose.staging.yml port mapping, so
# no env vars are needed when run against a locally-started staging-shaped stack.
#
# Requires curl and jq (both preinstalled on GitHub Actions ubuntu-latest runners).

set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:8080}"
CLIENT_BASE_URL="${CLIENT_BASE_URL:-http://localhost:4200}"
DEMO_PASSWORD="Passw0rd!23456"

fail() {
    echo "::error::$1" >&2
    exit 1
}

echo "==> Checking ${API_BASE_URL}/health/ready"
curl -sf "${API_BASE_URL}/health/ready" >/dev/null || fail "API readiness check failed (database/outbox/storage)"

echo "==> Resolving DEMO tenant"
tenant_id=$(curl -sf "${API_BASE_URL}/api/v1/tenants/by-code/DEMO" | jq -r '.tenantId')
[ -n "$tenant_id" ] && [ "$tenant_id" != "null" ] || fail "Could not resolve DEMO tenant id"

echo "==> Logging in as a seeded demo user (priya.sharma)"
device_id=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || python3 -c 'import uuid; print(uuid.uuid4())')
login_response=$(curl -sf -X POST "${API_BASE_URL}/api/v1/auth/login" \
    -H "Content-Type: application/json" \
    -H "X-Tenant-Id: ${tenant_id}" \
    -d "{\"email\":\"priya.sharma@demo.vespera.test\",\"password\":\"${DEMO_PASSWORD}\",\"deviceId\":\"${device_id}\"}")
access_token=$(echo "$login_response" | jq -r '.accessToken')
[ -n "$access_token" ] && [ "$access_token" != "null" ] || fail "Login did not return an access token: ${login_response}"

echo "==> Calling an authenticated endpoint with the issued token"
curl -sf "${API_BASE_URL}/api/v1/dashboard" -H "Authorization: Bearer ${access_token}" >/dev/null \
    || fail "Authenticated dashboard request failed"

echo "==> Checking the client is serving"
curl -sf "${CLIENT_BASE_URL}/" >/dev/null || fail "Client root did not respond"

echo "Smoke test passed: API ready, DEMO tenant resolvable, login works, dashboard reachable, client serving."
