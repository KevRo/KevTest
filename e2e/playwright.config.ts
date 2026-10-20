import { defineConfig, devices } from '@playwright/test';

const API_URL = 'http://localhost:5132';
// Port 5000 is claimed by macOS AirPlay Receiver (ControlCenter) on many machines
// and silently returns 403s to anything that hits it, so the web app uses 5090 here.
const WEB_URL = 'http://localhost:5090';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  // Strava e2e specs share one isolated SQLite file (strava.e2e.db) seeded/cleared in
  // beforeAll/afterAll; running spec files concurrently in separate workers would race
  // on it, so force everything onto a single worker.
  workers: 1,
  reporter: 'list',
  use: {
    baseURL: WEB_URL,
    trace: 'on-first-retry',
    // Pin the locale: the app negotiates its UI language (en/ga/it) from
    // Accept-Language, and the tests assert on English button text.
    locale: 'en-US',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: [
    {
      command:
        'node scripts/reset-db.js && dotnet run --project ../src/KevTest.Api --no-launch-profile --urls ' +
        API_URL,
      url: `${API_URL}/api/products`,
      // Always start a fresh instance: reusing a developer's already-running
      // dev server would point the suite at their real kevtest.db.
      reuseExistingServer: false,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__Default: 'Data Source=kevtest.e2e.db',
      },
    },
    {
      command:
        'node scripts/reset-strava-db.js && dotnet run --project ../MyMVC.NetApp --no-launch-profile --urls ' +
        WEB_URL,
      url: WEB_URL,
      reuseExistingServer: false,
      timeout: 120_000,
      env: {
        // 'E2E' (not 'Development') so Program.cs wires up FakeAskStravaService instead of
        // the real Anthropic-backed one - see MyMVC.NetApp/Services/FakeAskStravaService.cs.
        ASPNETCORE_ENVIRONMENT: 'E2E',
        ProductsApi__BaseUrl: API_URL,
        // Isolated from the developer's real strava.db - never touched by this suite.
        ConnectionStrings__Strava: 'Data Source=strava.e2e.db',
        // AskStravaController short-circuits to a ConfigMissing error before ever calling
        // IAskStravaService when this is blank (see AskStravaControllerTests.cs for that
        // path's coverage, since a shared server process can't be both blank and set at
        // once) - a dummy non-empty value here is what lets e2e tests reach
        // FakeAskStravaService's deterministic reply.
        Claude__ApiKey: 'e2e-dummy-key',
      },
    },
  ],
});
