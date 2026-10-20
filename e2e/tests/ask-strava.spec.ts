import { test, expect } from '@playwright/test';
import { clearStravaData, seedActivities, seedAthlete } from '../helpers/strava-db';

// The "Claude:ApiKey not configured" error path is covered at the unit level instead
// (tests/KevTest.Tests/AskStravaControllerTests.cs), not here: AskStravaController checks
// Claude:ApiKey before ever calling IAskStravaService, and this suite's single shared server
// process has a dummy key set (via playwright.config.ts) precisely so these tests can reach
// FakeAskStravaService - it can't simultaneously be blank for a ConfigMissing e2e test too.

test.describe('Ask Strava page', () => {
  const athleteId = 9101;

  test.beforeEach(() => {
    clearStravaData();
    seedAthlete({
      id: athleteId,
      firstname: 'Ada',
      lastname: 'Lovelace',
      allTimeDistanceMeters: 0,
      fetchedAtUtc: '2026-01-01 12:00:00',
    });
    seedActivities([
      { id: 1, athleteId, name: 'Morning Run', type: 'Run', distance: 5000, movingTime: 1500, startDateUtc: '2026-01-03 06:00:00', startDateLocal: '2026-01-03T06:00:00+00:00' },
      { id: 2, athleteId, name: 'Evening Ride', type: 'Ride', distance: 20000, movingTime: 3000, startDateUtc: '2026-01-02 18:00:00', startDateLocal: '2026-01-02T18:00:00+00:00' },
      { id: 3, athleteId, name: 'Track Session', type: 'Run', distance: 8000, movingTime: 2000, startDateUtc: '2026-01-01 07:00:00', startDateLocal: '2026-01-01T07:00:00+00:00' },
    ]);
  });

  test.afterEach(() => {
    clearStravaData();
  });

  test('asks a question and the answer persists across reload', async ({ page }) => {
    await page.goto('/AskStrava');
    await expect(page.locator('p', { hasText: 'No questions asked yet' })).toBeVisible();

    await page.fill('[name="question"]', 'How many activities do I have?');
    await page.click('#askStravaSubmit');

    await expect(page.locator('p.text-break').first()).toHaveText('How many activities do I have?');
    await expect(page.locator('p.text-break').nth(1)).toContainText('3 activities synced');
    await expect(page.locator('p.text-break').nth(1)).toContainText('How many activities do I have?');

    await page.reload();

    await expect(page.locator('p.text-break').first()).toHaveText('How many activities do I have?');
    await expect(page.locator('p.text-break').nth(1)).toContainText('3 activities synced');
  });

  test('clears the conversation', async ({ page }) => {
    await page.goto('/AskStrava');
    await page.fill('[name="question"]', 'Test question');
    await page.click('#askStravaSubmit');
    await expect(page.getByRole('button', { name: 'Clear conversation' })).toBeVisible();

    await page.getByRole('button', { name: 'Clear conversation' }).click();

    await expect(page.locator('p', { hasText: 'No questions asked yet' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Clear conversation' })).toHaveCount(0);
  });
});
