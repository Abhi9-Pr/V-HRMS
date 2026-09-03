// Load-tests the payroll dry-run endpoint (POST /api/v1/finance/payroll-runs/{id}/dry-run) —
// the handler this whole performance phase started from, after its N+1 fix (see
// RunDryRunCommandHandler.cs: batched to ~7 queries total regardless of headcount, was up to
// 4 * employeeCount before).
//
// Employee bulk-import (seed-employees.js) proves the import pipeline scales to 5,000 rows, but
// there's no bulk endpoint for salary structures — only one-at-a-time
// POST /api/v1/finance/salary-structures (this script's own gap-fill: see
// CreateSalaryComponentCommand's doc comment for why even *that* needed a new endpoint first).
// So this script seeds its own smaller, self-contained cohort of employees + salary structures
// in setup() rather than depending on seed-employees.js's 5,000 — EMPLOYEE_COUNT defaults to
// 500, high enough to meaningfully exercise the batched queries (their cost should stay flat as
// this scales, not grow linearly — that's the actual regression this script guards against),
// without paying for 5,000 sequential structure-creation HTTP calls just to set up the
// measurement. setup() runs exactly once, before the VUs start, and its return value is handed
// to every VU's default() call — the correct k6 pattern for "expensive shared setup," not a
// module-level variable (each VU gets its own JS VM, so module state isn't actually shared).
//
// Usage:
//   docker run --rm --network delivery-docker-foundation_default -v "$(pwd)/k6:/scripts" \
//     -e BASE_URL=http://api:8080 grafana/k6 run /scripts/payroll-dry-run.js
//
// Env vars: BASE_URL, EMPLOYEE_COUNT (default 500), VUS (default 5), DURATION (default 30s),
// ITERATIONS (if set, overrides VUS/DURATION with a single-VU run of exactly this many dry-run
// calls — the CI performance-budget job uses ITERATIONS=1 for one clean, uncontended sample; see
// .github/workflows/ci.yml's payroll-performance-budget job and docs/performance.md),
// PAYROLL_DRY_RUN_BUDGET_MS (default 5000 — fails the k6 run, and so the CI job, if the p95 dry-run
// duration exceeds this).

import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Trend } from 'k6/metrics';
import { computeCurrentTotp } from './lib/totp.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const EMPLOYEE_COUNT = parseInt(__ENV.EMPLOYEE_COUNT || '500', 10);
const BUDGET_MS = parseInt(__ENV.PAYROLL_DRY_RUN_BUDGET_MS || '5000', 10);

const DEMO_TENANT_CODE = 'DEMO';
const HR_ADMIN_EMAIL = 'rohan.verma@demo.vespera.test';
const FINANCE_ADMIN_EMAIL = 'vikram.nair@demo.vespera.test';
const DEMO_PASSWORD = 'Passw0rd!23456';
// Matches DevelopmentSeeder.FinanceAdminTotpSecretBase32 exactly.
const FINANCE_TOTP_SECRET = 'JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP';

const dryRunDuration = new Trend('payroll_dry_run_duration_ms', true);

// Two concurrent VUs dry-running the SAME payroll run race on PayrollRun's optimistic-concurrency
// RowVersion — one wins (204), the other gets an expected, correct 409 (see docs/performance.md).
// That's real, intentional protection, not a bug, but it makes VUS>1 unsuitable for a budget
// check: a 409 isn't a slow dry-run, so ITERATIONS mode forces vus=1 to get clean latency samples.
export const options = {
  scenarios: __ENV.ITERATIONS
    ? { default: { executor: 'shared-iterations', vus: 1, iterations: parseInt(__ENV.ITERATIONS, 10) } }
    : { default: { executor: 'constant-vus', vus: parseInt(__ENV.VUS || '5', 10), duration: __ENV.DURATION || '30s' } },
  setupTimeout: '5m', // seeding up to EMPLOYEE_COUNT salary structures one HTTP call at a time
  thresholds: {
    payroll_dry_run_duration_ms: [`p(95)<${BUDGET_MS}`],
  },
};

function jsonHeaders(token) {
  return { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } };
}

function loginFinanceAdmin(tenantId) {
  const code = computeCurrentTotp(FINANCE_TOTP_SECRET);
  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: FINANCE_ADMIN_EMAIL, password: DEMO_PASSWORD, deviceId: 'k6-payroll-device', totpCode: code }),
    { headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': tenantId } },
  );
  check(res, { 'finance admin login succeeded': (r) => r.status === 200 });
  if (res.status !== 200) fail(`finance admin login failed: ${res.status} ${res.body}`);
  return res.json('accessToken');
}

// Employee create/master-data-read needs Employees.Write/Departments.Read etc., which the seeded
// Finance role does not hold (see DevelopmentSeeder: Finance has Finance.Admin only). So this
// script needs two separate sessions: HR (no TOTP) for employee/master-data calls, Finance (TOTP)
// for everything under /api/v1/finance/*.
function loginHrAdmin(tenantId) {
  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: HR_ADMIN_EMAIL, password: DEMO_PASSWORD, deviceId: 'k6-payroll-hr-device', totpCode: null }),
    { headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': tenantId } },
  );
  check(res, { 'hr admin login succeeded': (r) => r.status === 200 });
  if (res.status !== 200) fail(`hr admin login failed: ${res.status} ${res.body}`);
  return res.json('accessToken');
}

// A large single-page fetch, not real pagination — DevelopmentSeeder's demo tenant only ever has
// a handful of departments/designations/locations (plus whatever seed-employees.js added), so one
// generously-sized page is enough to find a fixed, known-seeded value by field.
function findByField(listUrl, auth, field, value, kind) {
  const res = http.get(`${listUrl}?page=1&pageSize=200`, auth);
  if (res.status !== 200) fail(`${kind} list failed: ${res.status} ${res.body}`);
  const items = res.json('items') || res.json('Items') || [];
  const match = items.find((item) => item[field] === value);
  if (!match) fail(`no ${kind} found with ${field}='${value}' — checked ${items.length} items`);
  return match.id || match.Id;
}

/** Runs once, before any VU starts. Its return value is passed as `data` to every VU's default(). */
export function setup() {
  const tenantRes = http.get(`${BASE_URL}/api/v1/tenants/by-code/${DEMO_TENANT_CODE}`);
  if (tenantRes.status !== 200) fail(`tenant lookup failed: ${tenantRes.status} ${tenantRes.body}`);
  const tenantId = tenantRes.json('tenantId');

  const token = loginFinanceAdmin(tenantId);
  const auth = jsonHeaders(token);

  const hrToken = loginHrAdmin(tenantId);
  const hrAuth = jsonHeaders(hrToken);

  // Suffixed per run — SalaryComponent enforces a (TenantId, Name) uniqueness constraint, so a
  // fixed name would 500 with a duplicate-key error on any second run against the same database.
  const suffix = Date.now();
  const componentRes = http.post(
    `${BASE_URL}/api/v1/finance/salary-structures/components`,
    JSON.stringify({ name: `Load Test Basic ${suffix}`, componentType: 'Earning', isTaxable: true }),
    auth,
  );
  if (componentRes.status !== 200) fail(`salary component create failed: ${componentRes.status} ${componentRes.body}`);
  const componentId = componentRes.json();

  // Uses master data DevelopmentSeeder always creates for the demo tenant (fixed names, unlike
  // seed-employees.js which provisions its own suffixed master data) — this script's cohort just
  // needs valid references, not dedicated departments/designations/locations. Resolved by id here
  // (not via a code/title/name lookup) because the single-employee create endpoint below takes
  // Guids directly, unlike the CSV bulk-import pipeline.
  const departmentId = findByField(`${BASE_URL}/api/v1/departments`, hrAuth, 'code', 'ENG', 'department');
  const designationId = findByField(`${BASE_URL}/api/v1/designations`, hrAuth, 'title', 'Software Engineer', 'designation');
  const locationId = findByField(`${BASE_URL}/api/v1/locations`, hrAuth, 'name', 'Head Office', 'location');

  // Bulk CSV import (used by seed-employees.js) can't be used here — its only way to recover the
  // ids salary-structure creation needs afterward is GET /api/v1/employees, and that endpoint has
  // no search/filter support at all (PagedRequest.SortBy is a dead field the handler never reads,
  // and Criteria is hardcoded to tenant+not-deleted — see EmployeesPagedSpecification), so it
  // always returns the same LastName-sorted page regardless of what was just imported. A prior
  // version of this script relied on a `search=` query param the endpoint silently ignores, which
  // meant repeat runs collided on stale employee ids from earlier runs and got 409s on salary
  // structure creation. Single-create returns the id directly, so no lookup is needed.
  const employeeIds = [];
  for (let i = 1; i <= EMPLOYEE_COUNT; i++) {
    const code = `PAYROLL-${suffix}-${String(i).padStart(5, '0')}`;
    const createRes = http.post(
      `${BASE_URL}/api/v1/employees`,
      JSON.stringify({
        code, firstName: 'PayrollLoad', lastName: `Employee${i}`,
        workEmail: `payroll.loadtest.${suffix}.${i}@vespera.test`,
        phone: `+1415${String(3000000 + i).padStart(7, '0')}`,
        dateOfBirth: '1990-01-01', dateOfJoining: '2024-01-15',
        departmentId, designationId, locationId,
      }),
      hrAuth,
    );
    if (createRes.status !== 200) {
      fail(`employee create failed for ${code}: ${createRes.status} ${createRes.body}`);
    }
    employeeIds.push(createRes.json('id'));
  }

  for (const employeeId of employeeIds) {
    const structureRes = http.post(
      `${BASE_URL}/api/v1/finance/salary-structures`,
      JSON.stringify({
        employeeId,
        monthlyCtc: 60000,
        lines: [{ componentId, formulaKind: 'FixedAmount', fixedAmount: 60000, referenceComponentId: null, percent: null, sumComponentIds: null }],
        validFrom: '2024-01-01',
        validTo: null,
      }),
      auth,
    );
    if (structureRes.status !== 200) {
      fail(`salary structure create failed for ${employeeId}: ${structureRes.status} ${structureRes.body}`);
    }
  }

  const now = new Date();
  const openRes = http.post(
    `${BASE_URL}/api/v1/finance/payroll-runs`,
    JSON.stringify({ month: now.getUTCMonth() + 1, year: now.getUTCFullYear(), idempotencyKey: `k6-payroll-${suffix}` }),
    auth,
  );
  if (openRes.status !== 200) fail(`payroll run open failed: ${openRes.status} ${openRes.body}`);
  const payrollRunId = openRes.json();

  const freezeRes = http.post(
    `${BASE_URL}/api/v1/finance/payroll-runs/${payrollRunId}/freeze-attendance`,
    JSON.stringify({ overrideReason: 'k6 load test — freezing immediately regardless of the configured freeze day' }),
    auth,
  );
  if (freezeRes.status !== 204) fail(`freeze-attendance failed: ${freezeRes.status} ${freezeRes.body}`);

  console.log(`Payroll run ${payrollRunId} ready with ${employeeIds.length} employees carrying an active salary structure.`);
  return { payrollRunId, token };
}

export default function (data) {
  const res = http.post(`${BASE_URL}/api/v1/finance/payroll-runs/${data.payrollRunId}/dry-run`, null, jsonHeaders(data.token));
  check(res, { 'dry-run succeeded': (r) => r.status === 204 });
  if (res.status === 204) {
    dryRunDuration.add(res.timings.duration);
  }

  sleep(1);
}
