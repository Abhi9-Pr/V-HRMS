import { expect, test } from '@playwright/test';
import { computeCurrentTotp } from './support/totp';

// Defaults to the local `dotnet run --launch-profile https` target; CI's staging job overrides
// this to the plain-HTTP docker-compose port (see docs/deployment.md).
const API_BASE_URL = process.env['E2E_API_BASE_URL'] ?? 'https://localhost:7095';
// DevelopmentSeeder.FinanceAdminTotpSecretBase32 — Finance.Admin logins require TOTP (see
// LoginCommandHandler), pre-enrolled with this fixed secret for both demo Finance users.
const FINANCE_ADMIN_TOTP_SECRET = 'JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP';

test.describe('Payroll run', () => {
  test.beforeEach(async ({ request }) => {
    const reachable = await request
      .get(`${API_BASE_URL}/health/live`, { ignoreHTTPSErrors: true, timeout: 3000 })
      .then((response) => response.ok())
      .catch(() => false);

    test.skip(!reachable, `Vespera.Api is not reachable at ${API_BASE_URL} — start it to run this spec.`);
  });

  test('login as Finance and open a new payroll run', async ({ page }) => {
    await page.goto('/auth/login');
    await page.getByLabel('Tenant code').fill('DEMO');
    await page.getByLabel('Email').fill('vikram.nair@demo.vespera.test');
    await page.getByLabel('Password').fill('Passw0rd!23456');
    await page.getByRole('button', { name: 'Sign in' }).click();

    // Finance.Admin needs a second factor — the form only shows the code field after the first
    // submit comes back "auth.totp_required" (see login.component.ts's needsTotp signal).
    await expect(page.getByLabel('Authenticator code')).toBeVisible();
    await page.getByLabel('Authenticator code').fill(computeCurrentTotp(FINANCE_ADMIN_TOTP_SECRET));
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/dashboard$/);
    await page.goto('/payroll/runs');
    await expect(page.getByRole('heading', { name: 'Payroll Run' })).toBeVisible();

    // DevelopmentSeeder already opens a run for the current calendar month (used by
    // payslip-download.spec.ts) — pick a month/year one calendar year back, which is guaranteed
    // never to collide with it or with a previous run of this spec.
    const now = new Date();
    const month = now.getMonth() + 1;
    const year = now.getFullYear() - 1;

    await page.getByLabel('Month').fill(String(month));
    await page.getByLabel('Year').fill(String(year));
    await page.getByRole('button', { name: 'Open payroll run' }).click();

    await expect(page.getByText(`${month}/${year}`)).toBeVisible();
    await expect(page.getByText('Status:')).toContainText('Draft');
  });
});
