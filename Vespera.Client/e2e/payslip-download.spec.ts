import { expect, test } from '@playwright/test';
import { randomUUID } from 'crypto';
import { computeCurrentTotp } from './support/totp';

// Defaults to the local `dotnet run --launch-profile https` target; CI's staging job overrides
// this to the plain-HTTP docker-compose port (see docs/deployment.md).
const API_BASE_URL = process.env['E2E_API_BASE_URL'] ?? 'https://localhost:7095';
const FINANCE_ADMIN_TOTP_SECRET = 'JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP';
const DEMO_PASSWORD = 'Passw0rd!23456';

test.describe('Payslip download', () => {
  test.beforeEach(async ({ request }) => {
    const reachable = await request
      .get(`${API_BASE_URL}/health/live`, { ignoreHTTPSErrors: true, timeout: 3000 })
      .then((response) => response.ok())
      .catch(() => false);

    test.skip(!reachable, `Vespera.Api is not reachable at ${API_BASE_URL} — start it to run this spec.`);
  });

  test('finalize and publish the current payroll run, then generate and download a payslip', async ({ page, request }) => {
    // There's no payroll-runs list screen yet (see docs/testing.md's onboarding-frontend note for
    // the same kind of pre-existing gap) — DevelopmentSeeder already opens, dry-runs, and approves
    // one real payroll run for the current month/year with a computed line for Priya, specifically
    // so FinancePolicyWallTests (and this spec) don't need to drive a full salary-structure +
    // dry-run compute from empty data just to reach a publishable run. This API-only setup block
    // finds that seeded run's id and Priya's employeeId on it — Fatima's actual journey under test
    // (finalize, publish, generate, download) all happens through the real UI below.
    const tenantLookup = await request
      .get(`${API_BASE_URL}/api/v1/tenants/by-code/DEMO`, { ignoreHTTPSErrors: true })
      .then((response) => response.json());

    const login = await request
      .post(`${API_BASE_URL}/api/v1/auth/login`, {
        ignoreHTTPSErrors: true,
        headers: { 'X-Tenant-Id': tenantLookup.tenantId },
        data: {
          email: 'fatima.khan@demo.vespera.test',
          password: DEMO_PASSWORD,
          deviceId: randomUUID(),
          totpCode: computeCurrentTotp(FINANCE_ADMIN_TOTP_SECRET),
        },
      })
      .then((response) => response.json());

    const authHeaders = { Authorization: `Bearer ${login.accessToken}` };
    const now = new Date();

    const runsPage = await request
      .get(`${API_BASE_URL}/api/v1/finance/payroll-runs?page=1&pageSize=50`, { ignoreHTTPSErrors: true, headers: authHeaders })
      .then((response) => response.json());
    const seededRun = (runsPage.items as { id: string; month: number; year: number }[]).find(
      (run) => run.month === now.getMonth() + 1 && run.year === now.getFullYear(),
    );
    expect(seededRun, 'DevelopmentSeeder should have opened a payroll run for the current month/year').toBeTruthy();

    const runDetail = await request
      .get(`${API_BASE_URL}/api/v1/finance/payroll-runs/${seededRun!.id}`, { ignoreHTTPSErrors: true, headers: authHeaders })
      .then((response) => response.json());
    const employeeId = (runDetail.lines as { employeeId: string }[])[0]?.employeeId;
    expect(employeeId, 'the seeded run should carry at least one computed payroll line').toBeTruthy();

    // The actual journey under test starts here — real browser UI from this point on.
    await page.goto('/auth/login');
    await page.getByLabel('Tenant code').fill('DEMO');
    await page.getByLabel('Email').fill('fatima.khan@demo.vespera.test');
    await page.getByLabel('Password').fill(DEMO_PASSWORD);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByLabel('Authenticator code')).toBeVisible();
    await page.getByLabel('Authenticator code').fill(computeCurrentTotp(FINANCE_ADMIN_TOTP_SECRET));
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL(/\/dashboard$/);

    await page.goto(`/payroll/runs/${seededRun!.id}`);
    await expect(page.getByText('Status:')).toContainText('Approved');

    // The stepper's [selectedIndex] tracks the run's *current* status step (here, "Approved"),
    // one behind the step whose own tab holds the button for the *next* transition — "Finalize"
    // lives under the "Finalized" tab, "Publish" under "Published". [linear]="false" allows
    // switching to them directly without walking through every intermediate step.
    await page.getByRole('tab', { name: 'Finalized' }).click();
    await page.getByRole('button', { name: 'Finalize' }).click();
    await expect(page.getByText('Status:')).toContainText('Finalized');

    await page.getByRole('tab', { name: 'Published' }).click();
    await page.getByRole('button', { name: 'Publish' }).click();
    await expect(page.getByText('Status:')).toContainText('Published');
    await expect(page.getByText('Payslips can now be generated for this run.')).toBeVisible();

    await page.goto(`/payroll/runs/${seededRun!.id}/payslips/${employeeId}`);
    await expect(page.getByRole('heading', { name: 'Payslip' })).toBeVisible();

    await page.getByRole('button', { name: 'Generate payslip' }).click();
    await page.getByRole('button', { name: 'Get download link' }).click();

    await expect(page.getByRole('link', { name: /Open PDF/ })).toBeVisible();
  });
});
