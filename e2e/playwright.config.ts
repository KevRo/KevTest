import { defineConfig, devices } from '@playwright/test';

const API_URL = 'http://localhost:5132';
// Port 5000 is claimed by macOS AirPlay Receiver (ControlCenter) on many machines
// and silently returns 403s to anything that hits it, so the web app uses 5090 here.
const WEB_URL = 'http://localhost:5090';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  reporter: 'list',
  use: {
    baseURL: WEB_URL,
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../src/KevTest.Api --no-launch-profile --urls ' + API_URL,
      url: `${API_URL}/api/products`,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__Default: 'Data Source=kevtest.e2e.db',
      },
    },
    {
      command: 'dotnet run --project ../MyMVC.NetApp --no-launch-profile --urls ' + WEB_URL,
      url: WEB_URL,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ProductsApi__BaseUrl: API_URL,
      },
    },
  ],
});
