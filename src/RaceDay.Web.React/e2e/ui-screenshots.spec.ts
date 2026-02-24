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
    await page.goto('/');
    
    // Wait for the root element to be visible
    await page.waitForSelector('#root', { state: 'visible' });
    
    // Wait for network to be idle
    await page.waitForLoadState('networkidle');
    
    // Additional wait for React to fully render
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Small delay to ensure all styles are applied
    await page.waitForTimeout(500);
    
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
    await page.goto('/');
    
    // Wait for page to load
    await page.waitForSelector('#root', { state: 'visible' });
    await page.waitForLoadState('networkidle');
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Fill in athlete weight
    const weightInput = page.locator('input[type="number"]').first();
    await weightInput.fill('75');
    
    // Select sport type (if there's a select element)
    const sportSelect = page.locator('select').first();
    if (await sportSelect.isVisible()) {
      await sportSelect.selectOption({ label: 'Running' });
    }
    
    // Small delay for form updates
    await page.waitForTimeout(500);
    
    // Try to open product selector if available
    const addProductButton = page.locator('button:has-text("Add"), button:has-text("Select")').first();
    if (await addProductButton.isVisible()) {
      await addProductButton.click();
      await page.waitForTimeout(500);
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
    await page.goto('/');
    
    // Wait for page to load
    await page.waitForSelector('#root', { state: 'visible' });
    await page.waitForLoadState('networkidle');
    await page.waitForSelector('h1:has-text("Race Day Nutrition Planner")', { state: 'visible' });
    
    // Try to find and click a button that opens product selector
    const browseButton = page.locator('button:has-text("Browse"), button:has-text("Add Product"), button:has-text("Select Product")').first();
    
    if (await browseButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await browseButton.click();
      await page.waitForTimeout(1000);
      
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
