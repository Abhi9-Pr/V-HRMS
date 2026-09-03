import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

// Defaults to the local `dotnet run --launch-profile https` target; CI's staging job overrides
// this to the plain-HTTP docker-compose port (see docs/deployment.md).
const API_BASE_URL = process.env['E2E_API_BASE_URL'] ?? 'https://localhost:7095';

test.describe('Landing dashboard', () => {
  test.beforeEach(async ({ request, page }) => {
    // Same Docker-probe-skip pattern as departments.spec.ts — this spec needs the real API and
    // its seeded demo data (DevelopmentSeeder's announcement/celebration/event/to-do fixtures).
    const reachable = await request
      .get(`${API_BASE_URL}/health/live`, { ignoreHTTPSErrors: true, timeout: 3000 })
      .then((response) => response.ok())
      .catch(() => false);

    test.skip(!reachable, `Vespera.Api is not reachable at ${API_BASE_URL} — start it to run this spec.`);

    await page.goto('/auth/login');
    await page.getByLabel('Tenant code').fill('DEMO');
    await page.getByLabel('Email').fill('priya.sharma@demo.vespera.test');
    await page.getByLabel('Password').fill('Passw0rd!23456');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL(/\/dashboard$/);
  });

  test('renders the seeded widgets quickly, without a full-page error', async ({ page }) => {
    const start = Date.now();

    // At least one widget card (the shift tracker, always visible by default) should render well
    // under a second on seeded demo data — see GetDashboardQueryHandler's parallel fetch + cache.
    // The assertion itself uses a more CI-tolerant bound; the <1s target is a server-side design
    // property (one aggregated call, every widget fetched concurrently), not something a shared
    // CI runner's page-load timing can prove precisely.
    await expect(page.getByRole('heading', { name: 'My shift' })).toBeVisible({ timeout: 5000 });
    expect(Date.now() - start).toBeLessThan(5000);

    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByText('This widget is temporarily unavailable.')).toHaveCount(0);
  });

  test('has no detectable accessibility violations', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'My shift' })).toBeVisible();

    const results = await new AxeBuilder({ page }).include('body').analyze();

    expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([]);
  });

  test('the customize dialog (drag-drop reorder/show-hide/resize) is keyboard-operable and has no violations', async ({ page }) => {
    await page.getByRole('button', { name: 'Customize' }).click();
    await expect(page.getByRole('heading', { name: 'Customize dashboard' })).toBeVisible();

    // Keyboard-only reorder: focus a drag handle and lift/move/drop with the keyboard, CDK's
    // built-in drag-drop keyboard interaction (Enter/Space to lift, arrow keys to move, Enter/
    // Space to drop) — not a mouse drag.
    const handle = page.getByRole('button', { name: /^Reorder / }).first();
    await handle.focus();
    await page.keyboard.press('Enter');
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');

    const results = await new AxeBuilder({ page }).include('mat-dialog-container, .cdk-overlay-container').analyze();
    expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([]);

    await page.getByRole('button', { name: 'Cancel' }).click();
  });
});
