import { test, expect } from '@playwright/test';

/**
 * UI Screenshot Tests
 * Generates screenshots for both mobile and desktop viewports for PR preview
 */

// Disable animations for stable screenshots
const disableAnimations = `
  *, *::before, *::after {
    transition: none !important;
    animation: none !important;
  }
`;

test.describe('UI Screenshots', () => {
  test.beforeEach(async ({ page }) => {
    // Inject CSS to disable animations
    await page.addStyleTag({ content: disableAnimations });
  });

  test('homepage - initial state', async ({ page }, testInfo) => {
    // Navigate to the homepage
    await page.goto('/', { waitUntil: 'networkidle' });
    
    // Wait for the root element to be visible
    await page.waitForSelector('#root', { state: 'visible' });
    
    // Wait for React to fully render the main heading
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Wait for the form to be ready (athlete weight input should be present)
    await page.waitForSelector('input[type="number"]', { state: 'visible' });
    
    // Take screenshot with project name in path
    const projectName = testInfo.project.name;
    await page.screenshot({ 
      path: `test-results/screenshots/${projectName}/home.png`,
      fullPage: true 
    });
    
    // Verify basic elements are present
    await expect(page.locator('h1')).toContainText('Race Day Nutrition Planner');
  });

  test('form with products loaded', async ({ page }, testInfo) => {
    // Navigate to the homepage
    await page.goto('/', { waitUntil: 'networkidle' });
    
    // Wait for page to be fully loaded
    await page.waitForSelector('#root', { state: 'visible' });
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Wait for the athlete weight input to be visible
    const weightInput = page.locator('input[type="number"]').first();
    await weightInput.waitFor({ state: 'visible' });
    
    // Wait for products to load (brand filter select should have options)
    const brandFilter = page.locator('#brand-filter');
    const brandFilterCount = await brandFilter.count();
    
    if (brandFilterCount > 0) {
      // Wait for the brand filter to have more than just the default option
      await expect.poll(async () => {
        const options = await brandFilter.locator('option').count();
        return options;
      }, {
        message: 'Brand filter should load options',
        timeout: 15000,
      }).toBeGreaterThan(1);
    }
    
    // Fill in athlete weight
    await weightInput.fill('75');
    
    // Wait for any loading states to complete
    await page.waitForLoadState('networkidle');
    
    // Take screenshot
    const projectName = testInfo.project.name;
    await page.screenshot({ 
      path: `test-results/screenshots/${projectName}/form-filled.png`,
      fullPage: true 
    });
  });

  test('product selector with brand filter', async ({ page }, testInfo) => {
    // Navigate to the homepage
    await page.goto('/', { waitUntil: 'networkidle' });
    
    // Wait for page to load
    await page.waitForSelector('#root', { state: 'visible' });
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Wait for products to load by checking the brand filter
    const brandFilter = page.locator('#brand-filter');
    const brandFilterCount = await brandFilter.count();
    
    if (brandFilterCount > 0) {
      // Wait for options to load
      await expect.poll(async () => {
        const options = await brandFilter.locator('option').count();
        return options;
      }, {
        message: 'Brand filter should load options',
        timeout: 15000,
      }).toBeGreaterThan(1);
      
      // Select a different brand if available
      const options = await brandFilter.locator('option').allTextContents();
      if (options.length > 2) {
        // Select the second brand (index 1 is first brand after "All Brands")
        await brandFilter.selectOption({ index: 2 });
        await page.waitForLoadState('networkidle');
      }
    }
    
    // Take screenshot with brand filter visible
    const projectName = testInfo.project.name;
    await page.screenshot({ 
      path: `test-results/screenshots/${projectName}/product-selector.png`,
      fullPage: true 
    });
  });
});
