import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for UI screenshot tests
 * Generates both mobile and desktop screenshots for PR preview
 */
export default defineConfig({
  testDir: './e2e',
  
  // Maximum time one test can run
  timeout: 30 * 1000,
  
  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,
  
  // Retry on CI only
  retries: process.env.CI ? 2 : 0,
  
  // Opt out of parallel tests on CI
  workers: process.env.CI ? 1 : undefined,
  
  // Reporter configuration
  reporter: [
    ['html', { 
      open: 'never',
      outputFolder: 'playwright-report' 
    }],
    ['list']
  ],
  
  use: {
    // Base URL to test against
    baseURL: process.env.BASE_URL || 'http://127.0.0.1:4173',
    
    // Collect trace on failure
    trace: 'retain-on-failure',
    
    // Collect video on failure (optional)
    video: 'retain-on-failure',
    
    // Maximum time each action can take
    actionTimeout: 10 * 1000,
  },

  // Define two projects for mobile and desktop screenshots
  projects: [
    {
      name: 'mobile',
      use: {
        ...devices['iPhone 13'],
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true,
      },
    },
    {
      name: 'desktop',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1440, height: 900 },
        isMobile: false,
      },
    },
  ],

  // Run your local dev server before starting the tests
  webServer: process.env.CI ? undefined : {
    command: 'npm run preview',
    url: 'http://127.0.0.1:4173',
    reuseExistingServer: !process.env.CI,
    timeout: 120 * 1000,
  },
});
