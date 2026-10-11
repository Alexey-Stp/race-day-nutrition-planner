import { test, expect, type Page } from '@playwright/test';

/**
 * UI screenshot tests for PR previews (mobile + desktop projects, see
 * playwright.config.ts). Screenshots land in test-results/screenshots/{project}/.
 */

// Disable animations for stable screenshots
const disableAnimations = `
  *, *::before, *::after {
    transition: none !important;
    animation: none !important;
  }
`;

async function openApp(page: Page) {
  await page.goto('/', { waitUntil: 'networkidle' });
  await page.addStyleTag({ content: disableAnimations });
  await page.waitForSelector('#root', { state: 'visible' });
  await expect(page.getByRole('heading', { level: 1, name: /Race Day Nutrition/ })).toBeVisible();
}

async function waitForBrands(page: Page) {
  const brandFilter = page.locator('#brand-filter');
  await expect
    .poll(() => brandFilter.locator('option').count(), {
      message: 'Brand filter should load options',
      timeout: 15000,
    })
    .toBeGreaterThan(1);
  return brandFilter;
}

test('home page', async ({ page }, testInfo) => {
  await openApp(page);
  await page.screenshot({
    path: `test-results/screenshots/${testInfo.project.name}/home.png`,
    fullPage: true,
  });
});

test('form filled', async ({ page }, testInfo) => {
  await openApp(page);
  const weightInput = page.locator('#weight');
  await weightInput.waitFor({ state: 'visible' });
  await waitForBrands(page);
  await weightInput.fill('75');
  await page.waitForLoadState('networkidle');
  await page.screenshot({
    path: `test-results/screenshots/${testInfo.project.name}/form-filled.png`,
    fullPage: true,
  });
});

test('product selector with brand filter', async ({ page }, testInfo) => {
  await openApp(page);
  const brandFilter = await waitForBrands(page);
  // Index 0 is "All brands"; pick a second brand when one exists.
  if ((await brandFilter.locator('option').count()) > 2) {
    await brandFilter.selectOption({ index: 2 });
    await page.waitForLoadState('networkidle');
  }
  await page.screenshot({
    path: `test-results/screenshots/${testInfo.project.name}/product-selector.png`,
    fullPage: true,
  });
});
