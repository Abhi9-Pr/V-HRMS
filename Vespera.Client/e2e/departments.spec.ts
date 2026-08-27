import { expect, test } from '@playwright/test';

const API_BASE_URL = 'https://localhost:7095';

test.describe('Departments CRUD', () => {
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

  test('login, create a department, and see it in the list', async ({ page }) => {
    const departmentName = `Quality Assurance ${Date.now()}`;
    const departmentCode = `QA${Date.now()}`.slice(-10);

    await page.goto('/auth/login');
    await page.getByLabel('Tenant code').fill('DEMO');
    await page.getByLabel('Email').fill('rohan.verma@demo.vespera.test');
    await page.getByLabel('Password').fill('Passw0rd!23456');
    await page.getByRole('button', { name: 'Sign in' }).click();

    // Login redirects to '/', which resolves to the landing dashboard, not straight to
    // Departments — navigate there explicitly.
    await expect(page).toHaveURL(/\/dashboard$/);
    await page.goto('/departments');
    await expect(page.getByRole('heading', { name: 'Departments' })).toBeVisible();

    await page.getByRole('button', { name: 'New department' }).click();
    await page.getByLabel('Name').fill(departmentName);
    await page.getByLabel('Code').fill(departmentCode);
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByRole('cell', { name: departmentName })).toBeVisible();
  });
});
