import { expect, test } from '@playwright/test';

const API_BASE_URL = 'https://localhost:7095';

test.describe('Attendance punch', () => {
  test.beforeEach(async ({ request }) => {
    // Mirrors the backend's own Docker-probe-skip pattern: this spec needs the real API (and its
    // seeded demo data) running — see docs/CONTRIBUTING-frontend.md for how to start it. Skip
    // gracefully rather than failing the whole suite when it isn't up.
    const reachable = await request
      .get(`${API_BASE_URL}/health/live`, { ignoreHTTPSErrors: true, timeout: 3000 })
      .then((response) => response.ok())
      .catch(() => false);

    test.skip(!reachable, `Vespera.Api is not reachable at ${API_BASE_URL} — start it to run this spec.`);
  });

  test('login and punch in/out from the dashboard shift-tracker widget', async ({ page }) => {
    await page.goto('/auth/login');
    await page.getByLabel('Tenant code').fill('DEMO');
    await page.getByLabel('Email').fill('priya.sharma@demo.vespera.test');
    await page.getByLabel('Password').fill('Passw0rd!23456');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/dashboard$/);

    // The shift-tracker widget is on by default (ShiftTrackerWidgetProvider.DefaultVisible) —
    // scope into its <section role="listitem"> by its registry title, "My shift", the same way
    // the customize dialog in dashboard.spec.ts scopes into widget entries.
    const shiftTrackerWidget = page.getByRole('listitem').filter({ hasText: 'My shift' });
    await expect(shiftTrackerWidget).toBeVisible();

    const punchButton = shiftTrackerWidget.getByRole('button', { name: /Punch (in|out)/ });
    await expect(punchButton).toBeVisible();

    // Whatever state today's seed data leaves the employee in (not punched in yet, or already
    // punched in from a previous run of this spec), the button's own label says which action is
    // next — click it and assert the status pill flips to match, rather than assuming a fixed
    // starting state.
    const initialLabel = await punchButton.textContent();
    const punchingIn = initialLabel?.includes('Punch in') ?? true;

    await punchButton.click();

    if (punchingIn) {
      await expect(shiftTrackerWidget.getByText('Punched in')).toBeVisible();
      await expect(shiftTrackerWidget.getByRole('button', { name: 'Punch out' })).toBeVisible();
    } else {
      await expect(shiftTrackerWidget.getByText('Punched out')).toBeVisible();
      await expect(shiftTrackerWidget.getByRole('button', { name: 'Punch in' })).toBeVisible();
    }
  });
});
