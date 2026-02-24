# UI Screenshot Testing - Configuration

## Port Configuration

The screenshot testing infrastructure uses specific ports for local development and CI:

- **Preview Server**: `4173` (Vite preview mode)
  - Configured in: `playwright.config.ts`, `vite.config.ts`, `.github/workflows/pr_check.yml`
- **API Server**: `5208` (ASP.NET Core)
  - Configured in: `src/RaceDay.API/Properties/launchSettings.json`, `.github/workflows/pr_check.yml`
  - Proxied through Vite for CORS

## How Screenshot Tests Work

1. **CI Workflow** (`.github/workflows/pr_check.yml`):
   - Builds both backend and frontend
   - Starts API server on port 5208
   - Starts Vite preview server on port 4173
   - Runs Playwright tests for mobile and desktop
   - Uploads results to GitHub Pages
   - Posts PR comment with screenshots

2. **Playwright Config** (`playwright.config.ts`):
   - Defines two projects: `mobile` (390×844) and `desktop` (1440×900)
   - Screenshots saved to `test-results/screenshots/{project}/`
   - HTML report generated in `playwright-report/`

3. **Test Files** (`e2e/ui-screenshots.spec.ts`):
   - Disables animations for stable screenshots
   - Uses deterministic waits (no hardcoded timeouts)
   - Captures multiple UI states

## Running Locally

```bash
# Terminal 1: Start API server
cd /path/to/race-day-nutrition-planner
dotnet run --project src/RaceDay.API/RaceDay.API.csproj

# Terminal 2: Start frontend
cd src/RaceDay.Web.React
npm install
npm run build
npm run preview

# Terminal 3: Run tests
cd src/RaceDay.Web.React
npx playwright test
npx playwright show-report
```

## Adding New Screenshots

To add new screenshot tests:

1. Edit `e2e/ui-screenshots.spec.ts`
2. Create a new test case
3. Use `testInfo.project.name` to separate mobile/desktop paths
4. Ensure proper waits before screenshots:
   ```typescript
   await page.goto('/', { waitUntil: 'networkidle' });
   await page.waitForSelector('#expected-element', { state: 'visible' });
   await page.screenshot({ 
     path: `test-results/screenshots/${testInfo.project.name}/my-new-screenshot.png`,
     fullPage: true 
   });
   ```
