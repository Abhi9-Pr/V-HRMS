import { expect, test } from '@playwright/test';

test('the app loads and the login form renders', async ({ page }) => {
  await page.goto('/auth/login');

  await expect(page.getByText('Sign in to Vespera')).toBeVisible();
  await expect(page.getByLabel('Tenant code')).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
});

test('an unauthenticated visit to a protected route redirects to login', async ({ page }) => {
  await page.goto('/departments');

  await expect(page).toHaveURL(/\/auth\/login/);
});
