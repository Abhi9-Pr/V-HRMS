// Load-tests GET /api/v1/attendance/grid — the new endpoint this performance phase added
// (GetAttendanceGridQueryHandler.cs). Two flat projected queries per request regardless of page
// size (employees, then their AttendanceDay cells) — this measures whether that actually holds
// up under concurrent load and at a realistic page size, not just correctness (already covered
// by AttendanceGridCrudTests.cs).
//
// Run seed-employees.js first for this to exercise a realistic employee count — against just the
// 5 DevelopmentSeeder demo employees the grid query is trivially fast regardless of whether the
// N+1 fix pattern was applied correctly.
//
// Usage:
//   docker run --rm --network delivery-docker-foundation_default -v "$(pwd)/k6:/scripts" \
//     -e BASE_URL=http://api:8080 grafana/k6 run /scripts/attendance-grid.js
//
// Env vars: BASE_URL, VUS (default 10), DURATION (default 30s), PAGE_SIZE (default 200).

import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const PAGE_SIZE = parseInt(__ENV.PAGE_SIZE || '200', 10);
const DEMO_TENANT_CODE = 'DEMO';
const DEMO_PASSWORD = 'Passw0rd!23456';

const gridDuration = new Trend('attendance_grid_duration_ms', true);

export const options = {
  vus: parseInt(__ENV.VUS || '10', 10),
  duration: __ENV.DURATION || '30s',
};

export function setup() {
  const tenantRes = http.get(`${BASE_URL}/api/v1/tenants/by-code/${DEMO_TENANT_CODE}`);
  if (tenantRes.status !== 200) fail(`tenant lookup failed: ${tenantRes.status} ${tenantRes.body}`);
  const tenantId = tenantRes.json('tenantId');

  // Attendance.ReadTeam isn't granted to the ordinary HR role in DevelopmentSeeder (only the
  // full sysadmin role has it — see AttendanceGridCrudTests.cs's own note on this), so this
  // needs the sysadmin login, not the usual HR one every other script here uses.
  const loginRes = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: 'admin@demo.vespera.test', password: DEMO_PASSWORD, deviceId: 'k6-grid-device', totpCode: null }),
    { headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': tenantId } },
  );
  if (loginRes.status !== 200) fail(`login failed: ${loginRes.status} ${loginRes.body}`);

  return { token: loginRes.json('accessToken') };
}

export default function (data) {
  const rangeStart = '2026-06-01';
  const rangeEnd = '2026-06-07'; // 7 days — comfortably under the query validator's 31-day cap
  const res = http.get(
    `${BASE_URL}/api/v1/attendance/grid?rangeStart=${rangeStart}&rangeEnd=${rangeEnd}&page=1&pageSize=${PAGE_SIZE}`,
    { headers: { Authorization: `Bearer ${data.token}` } },
  );
  check(res, { 'attendance grid succeeded': (r) => r.status === 200 });
  if (res.status === 200) {
    gridDuration.add(res.timings.duration);
  }

  sleep(1);
}
