import { expect, test } from '@playwright/test';

// Defaults to the local `dotnet run --launch-profile https` target; CI's staging job overrides
// this to the plain-HTTP docker-compose port (see docs/deployment.md).
const API_BASE_URL = process.env['E2E_API_BASE_URL'] ?? 'https://localhost:7095';

/** M/D/YYYY — Angular Material's date-range input under the default en-US locale (see
 * app.config.ts: no MAT_DATE_LOCALE override), matching how a person would type it. */
function formatForDatePicker(date: Date): string {
  return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear()}`;
}

test.describe('Leave request', () => {
  test.beforeEach(async ({ request }) => {
    const reachable = await request
      .get(`${API_BASE_URL}/health/live`, { ignoreHTTPSErrors: true, timeout: 3000 })
      .then((response) => response.ok())
      .catch(() => false);

    test.skip(!reachable, `Vespera.Api is not reachable at ${API_BASE_URL} — start it to run this spec.`);
  });

  test('login and submit a leave request', async ({ page }) => {
    await page.goto('/auth/login');
    await page.getByLabel('Tenant code').fill('DEMO');
    await page.getByLabel('Email').fill('priya.sharma@demo.vespera.test');
    await page.getByLabel('Password').fill('Passw0rd!23456');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/dashboard$/);

    // Wait for the leave-types fetch to land before opening the dropdown — LeaveFacade.loadLeaveTypes
    // populates it asynchronously on init, and opening the mat-select before it resolves opens an
    // empty panel that never gets the options inserted once they do arrive.
    const leaveTypesLoaded = page.waitForResponse((response) => response.url().includes('/api/v1/leave-types'));
    await page.goto('/leave/apply');
    await expect(page.getByRole('heading', { name: 'Apply for leave' })).toBeVisible();
    await leaveTypesLoaded;

    // Sick Leave's seeded policy allows the balance to go negative outright (no loss-of-pay
    // acknowledgement branch to navigate) — the most frictionless type for a one-day request from
    // a freshly-opened (zero) balance, see DevelopmentSeeder's LeavePolicy.ConfigureBalanceRules.
    await page.getByLabel('Leave type').click();
    await page.getByRole('option', { name: 'Sick Leave' }).click();

    // A single weekday about two weeks out — comfortably clear of the seeded Dec 24–Jan 2
    // blackout period regardless of when this spec runs.
    const start = new Date();
    start.setDate(start.getDate() + 14);
    while (start.getDay() === 0 || start.getDay() === 6) {
      start.setDate(start.getDate() + 1);
    }
    const dateText = formatForDatePicker(start);

    await page.getByPlaceholder('Start date').fill(dateText);
    await page.getByPlaceholder('End date').fill(dateText);
    await page.getByPlaceholder('End date').press('Tab');

    await page.getByLabel('Reason').fill('E2E: one-day sick leave request');
    await page.getByRole('button', { name: 'Submit request' }).click();

    await expect(page.getByRole('status').filter({ hasText: 'Submitted' })).toBeVisible();
  });
});
