import { test } from '@playwright/test';

test('home page', async ({ page }, testInfo) => {
  const device = testInfo.project.name;
  await page.goto('/');
  await page.screenshot({
    path: `test-results/screenshots/${device}/home.png`,
    fullPage: true
  });
});

test('form filled', async ({ page }, testInfo) => {
  const device = testInfo.project.name;
  await page.goto('/');
  await page.waitForSelector('#root', { state: 'visible' });
  await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
  const weightInput = page.locator('input[type="number"]').first();
  await weightInput.waitFor({ state: 'visible' });
  await weightInput.fill('75');
  await page.waitForLoadState('networkidle');
  await page.screenshot({
    path: `test-results/screenshots/${device}/form-filled.png`,
    fullPage: true
  });
});
