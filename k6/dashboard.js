// Load-tests GET /api/v1/dashboard — GetDashboardQueryHandler fans out to every visible widget
// provider concurrently (Task.WhenAll, each with its own 5s timeout + 30s cache — see
// GetDashboardQueryHandler.cs), so this is really load-testing that fan-out plus every
// provider's own query shape at once, not a single simple read.
//
// Usage:
//   docker run --rm --network delivery-docker-foundation_default -v "$(pwd)/k6:/scripts" \
//     -e BASE_URL=http://api:8080 grafana/k6 run /scripts/dashboard.js
//
// Env vars: BASE_URL, VUS (default 10), DURATION (default 30s).

import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const DEMO_TENANT_CODE = 'DEMO';
const DEMO_PASSWORD = 'Passw0rd!23456';

// One login per VU (not per request) so the load measures the dashboard read itself, not the
// auth path — see setup()/default(data) split.
const dashboardDuration = new Trend('dashboard_duration_ms', true);

export const options = {
  vus: parseInt(__ENV.VUS || '10', 10),
  duration: __ENV.DURATION || '30s',
};

export function setup() {
  const tenantRes = http.get(`${BASE_URL}/api/v1/tenants/by-code/${DEMO_TENANT_CODE}`);
  if (tenantRes.status !== 200) fail(`tenant lookup failed: ${tenantRes.status} ${tenantRes.body}`);
  const tenantId = tenantRes.json('tenantId');

  const loginRes = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: 'rohan.verma@demo.vespera.test', password: DEMO_PASSWORD, deviceId: 'k6-dashboard-device', totpCode: null }),
    { headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': tenantId } },
  );
  if (loginRes.status !== 200) fail(`login failed: ${loginRes.status} ${loginRes.body}`);

  return { token: loginRes.json('accessToken') };
}

export default function (data) {
  const res = http.get(`${BASE_URL}/api/v1/dashboard`, { headers: { Authorization: `Bearer ${data.token}` } });
  check(res, { 'dashboard succeeded': (r) => r.status === 200 });
  if (res.status === 200) {
    dashboardDuration.add(res.timings.duration);
  }

  sleep(1);
}
