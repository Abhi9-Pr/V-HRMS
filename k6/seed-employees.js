// Seeds load-test data through the real HTTP API, not a direct-DB script — this is the same
// bulk CSV/Excel import pipeline a real HR admin uses (Vespera.Api/Controllers/V1/
// EmployeeImportController.cs), reused here rather than hand-rolled, because that pipeline
// already does the interesting part correctly: one master-data lookup pass + one SaveChanges per
// import batch, not one write per row (see docs/CONTRIBUTING-slices.md's Performance notes).
//
// Usage (against a running docker-compose stack):
//   docker run --rm --network delivery-docker-foundation_default -v "$(pwd)/k6:/scripts" \
//     -e BASE_URL=http://api:8080 grafana/k6 run /scripts/seed-employees.js
//
// Env vars: BASE_URL (default http://localhost:8080), EMPLOYEE_COUNT (default 5000),
// CHUNK_SIZE (default 500 — rows per import call; keeps any one request body reasonably sized
// and mirrors how a real bulk upload would be chunked rather than one giant file).

import http from 'k6/http';
import { check, fail } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const EMPLOYEE_COUNT = parseInt(__ENV.EMPLOYEE_COUNT || '5000', 10);
const CHUNK_SIZE = parseInt(__ENV.CHUNK_SIZE || '500', 10);
const DEPARTMENT_COUNT = 30;
const DESIGNATION_COUNT = 15;
const LOCATION_COUNT = 10;

// Matches DevelopmentSeeder.DemoTenantCode / DemoPassword exactly — this script seeds *on top
// of* the existing demo tenant, not a fresh one, so the load-test data shares master data
// (roles/permissions) already set up for the demo HR admin.
const DEMO_TENANT_CODE = 'DEMO';
const HR_ADMIN_EMAIL = 'rohan.verma@demo.vespera.test';
const DEMO_PASSWORD = 'Passw0rd!23456';

export const options = { vus: 1, iterations: 1 };

function jsonHeaders(token) {
  return { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } };
}

function resolveTenantId() {
  const res = http.get(`${BASE_URL}/api/v1/tenants/by-code/${DEMO_TENANT_CODE}`);
  check(res, { 'tenant lookup succeeded': (r) => r.status === 200 });
  if (res.status !== 200) fail(`tenant lookup failed: ${res.status} ${res.body}`);
  return res.json('tenantId');
}

function login(tenantId) {
  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: HR_ADMIN_EMAIL, password: DEMO_PASSWORD, deviceId: 'k6-seed-device', totpCode: null }),
    { headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': tenantId } },
  );
  check(res, { 'login succeeded': (r) => r.status === 200 });
  if (res.status !== 200) fail(`login failed: ${res.status} ${res.body}`);
  return res.json('accessToken');
}

function createMasterData(token) {
  const auth = jsonHeaders(token);
  // Suffixed per run — neither Department.Code, Designation.Title, nor Location.Name is
  // enforced unique today, so re-running this script against a database that already has an
  // earlier run's master data would otherwise create silent duplicates (harmless on their own,
  // but ImportEmployeesCommandHandler resolves CSV references by exactly these fields, and used
  // to crash outright on a duplicate — see its own fix in the same phase as this script).
  // Department.Code has a 20-char max (server-side validation) — "LOADDEPT-" (9) + suffix +
  // "-001" (4) leaves at most 7 chars, so this uses the last 6 digits of the timestamp rather
  // than the full millisecond value.
  const runSuffix = String(Date.now()).slice(-6);

  const departmentCodes = [];
  for (let i = 1; i <= DEPARTMENT_COUNT; i++) {
    const code = `LOADDEPT-${runSuffix}-${String(i).padStart(3, '0')}`;
    const res = http.post(
      `${BASE_URL}/api/v1/departments`,
      JSON.stringify({ name: `Load Test Dept ${runSuffix}-${i}`, code, parentDepartmentId: null }),
      auth,
    );
    if (res.status !== 200) {
      fail(`department create failed for ${code}: ${res.status} ${res.body}`);
    }
    departmentCodes.push(code);
  }

  const designationTitles = [];
  for (let i = 1; i <= DESIGNATION_COUNT; i++) {
    const title = `Load Test Title ${runSuffix}-${i}`;
    const res = http.post(`${BASE_URL}/api/v1/designations`, JSON.stringify({ title, grade: (i % 5) + 1 }), auth);
    if (res.status !== 200) {
      fail(`designation create failed for ${title}: ${res.status} ${res.body}`);
    }
    designationTitles.push(title);
  }

  const locationNames = [];
  for (let i = 1; i <= LOCATION_COUNT; i++) {
    const name = `Load Test Campus ${runSuffix}-${i}`;
    const res = http.post(
      `${BASE_URL}/api/v1/locations`,
      JSON.stringify({
        name, addressLine: `${i} Load Test Way`, city: 'Bengaluru', country: 'India',
        latitude: 12.9716 + i * 0.001, longitude: 77.5946 + i * 0.001, timeZoneId: 'Asia/Kolkata',
      }),
      auth,
    );
    if (res.status !== 200) {
      fail(`location create failed for ${name}: ${res.status} ${res.body}`);
    }
    locationNames.push(name);
  }

  return { departmentCodes, designationTitles, locationNames };
}

function buildCsvChunk(startIndex, count, masterData) {
  const lines = ['Code,FirstName,LastName,WorkEmail,Phone,DateOfBirth,DateOfJoining,DepartmentCode,DesignationTitle,LocationName'];
  for (let i = 0; i < count; i++) {
    const n = startIndex + i;
    const code = `LOAD-${String(n).padStart(6, '0')}`;
    const phoneSuffix = String(2000000 + n).padStart(7, '0');
    const dept = masterData.departmentCodes[n % masterData.departmentCodes.length];
    const designation = masterData.designationTitles[n % masterData.designationTitles.length];
    const location = masterData.locationNames[n % masterData.locationNames.length];
    lines.push(
      [
        code, 'LoadTest', `Employee${n}`, `loadtest.employee${n}@vespera.test`, `+1415${phoneSuffix}`,
        '1990-01-01', '2024-01-15', dept, designation, location,
      ].join(','),
    );
  }
  return lines.join('\n');
}

export default function () {
  const tenantId = resolveTenantId();
  const token = login(tenantId);
  const masterData = createMasterData(token);

  let imported = 0;
  for (let start = 1; start <= EMPLOYEE_COUNT; start += CHUNK_SIZE) {
    const count = Math.min(CHUNK_SIZE, EMPLOYEE_COUNT - start + 1);
    const csv = buildCsvChunk(start, count, masterData);

    const res = http.post(
      `${BASE_URL}/api/v1/employees/import?dryRun=false`,
      { file: http.file(csv, `load-test-employees-${start}.csv`, 'text/csv') },
      { headers: { Authorization: `Bearer ${token}` } },
    );

    if (res.status !== 200) {
      fail(`import chunk starting at ${start} failed: ${res.status} ${res.body}`);
    }

    const report = res.json();
    if (!report.committed) {
      fail(`import chunk starting at ${start} did not commit: ${JSON.stringify(report)}`);
    }
    imported += report.successCount;
    console.log(`Imported rows ${start}-${start + count - 1}: successCount=${report.successCount} failureCount=${report.failureCount}`);
  }

  console.log(`Seeded ${imported} employees across ${DEPARTMENT_COUNT} departments / ${DESIGNATION_COUNT} designations / ${LOCATION_COUNT} locations.`);
}
