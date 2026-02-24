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

  test('form with products selected', async ({ page }, testInfo) => {
    // Navigate to the homepage
    await page.goto('/', { waitUntil: 'networkidle' });
    
    // Wait for page to load
    await page.waitForSelector('#root', { state: 'visible' });
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Fill in athlete weight and wait for it to be populated
    const weightInput = page.locator('input[type="number"]').first();
    await weightInput.waitFor({ state: 'visible' });
    await weightInput.fill('75');
    
    // Select sport type (if there's a select element)
    const sportSelect = page.locator('select').first();
    if (await sportSelect.isVisible({ timeout: 1000 }).catch(() => false)) {
      await sportSelect.selectOption({ label: 'Running' });
    }
    
    // Try to open product selector if available
    const addProductButton = page.locator('button:has-text("Add"), button:has-text("Select")').first();
    if (await addProductButton.isVisible({ timeout: 1000 }).catch(() => false)) {
      await addProductButton.click();
      // Wait for any modal or product list to appear
      await page.waitForLoadState('networkidle');
    }
    
    // Take screenshot
    const projectName = testInfo.project.name;
    await page.screenshot({ 
      path: `test-results/screenshots/${projectName}/form-filled.png`,
      fullPage: true 
    });
  });

  test('product selector modal', async ({ page }, testInfo) => {
    // Navigate to the homepage
    await page.goto('/', { waitUntil: 'networkidle' });
    
    // Wait for page to load
    await page.waitForSelector('#root', { state: 'visible' });
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Try to find and click a button that opens product selector
    const browseButton = page.locator('button:has-text("Browse"), button:has-text("Add Product"), button:has-text("Select Product")').first();
    
    if (await browseButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await browseButton.click();
      
      // Wait for modal or product list to be visible (using a generic approach)
      await page.waitForLoadState('networkidle');
      
      // Take screenshot with modal open
      const projectName = testInfo.project.name;
      await page.screenshot({ 
        path: `test-results/screenshots/${projectName}/product-selector.png`,
        fullPage: true 
      });
    } else {
      // If no modal, just take a screenshot of current state
      const projectName = testInfo.project.name;
      await page.screenshot({ 
        path: `test-results/screenshots/${projectName}/product-selector.png`,
        fullPage: true 
      });
    }
  });
});
