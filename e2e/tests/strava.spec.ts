import { test, expect } from '@playwright/test';
import { clearStravaData, seedActivities, seedAthlete } from '../helpers/strava-db';

test.describe('Strava page - not configured / OAuth error states (no seeding needed)', () => {
  test('shows a config-missing error when Strava credentials are not set', async ({ page }) => {
    await page.goto('/Strava');
    await expect(page.locator('p', { hasText: 'Not connected to Strava yet.' })).toBeVisible();

    await page.click('#stravaConnectBtn');

    await expect(page.locator('.alert-danger')).toHaveText(
      "Strava isn't configured yet. Set Strava:ClientId and Strava:ClientSecret (see README) before connecting.",
    );
  });

  test('shows a denied error when the OAuth callback reports an error', async ({ page }) => {
    await page.goto('/Strava/Callback?error=access_denied');

    await expect(page).toHaveURL(/\/Strava$/);
    await expect(page.locator('.alert-danger')).toHaveText('Strava authorization was cancelled or denied.');
  });

  test('shows an invalid-state error when the callback state does not match', async ({ page }) => {
    // No prior /Strava/Connect call in this test means no state cookie was ever set,
    // so any code/state pair here is guaranteed to fail StravaController's state check.
    await page.goto('/Strava/Callback?code=x&state=wrong');

    await expect(page).toHaveURL(/\/Strava$/);
    await expect(page.locator('.alert-danger')).toHaveText(
      'Could not verify the Strava sign-in response. Please try again.',
    );
  });
});

test.describe('Strava page - connected (seeded athlete + activities)', () => {
  const athleteId = 9001;

  test.beforeEach(() => {
    clearStravaData();
  });

  test.afterEach(() => {
    clearStravaData();
  });

  test('shows the connected status card', async ({ page }) => {
    seedAthlete({
      id: athleteId,
      firstname: 'Ada',
      lastname: 'Lovelace',
      city: 'London',
      country: 'United Kingdom',
      allTimeDistanceMeters: 123456,
      fetchedAtUtc: '2026-01-01 12:00:00',
    });
    seedActivities([
      { id: 1, athleteId, name: 'Morning Run', type: 'Run', distance: 5000, movingTime: 1500, startDateUtc: '2026-01-03 06:00:00', startDateLocal: '2026-01-03T06:00:00+00:00' },
      { id: 2, athleteId, name: 'Evening Ride', type: 'Ride', distance: 20000, movingTime: 3000, startDateUtc: '2026-01-02 18:00:00', startDateLocal: '2026-01-02T18:00:00+00:00' },
      { id: 3, athleteId, name: 'Track Session', type: 'Run', distance: 8000, movingTime: 2000, startDateUtc: '2026-01-01 07:00:00', startDateLocal: '2026-01-01T07:00:00+00:00' },
    ]);

    await page.goto('/Strava');

    await expect(page.locator('h5.mb-0')).toHaveText('Ada Lovelace');
    await expect(page.locator('p', { hasText: 'Activities stored' })).toContainText('3');
    await expect(page.locator('p', { hasText: 'All-time distance' })).toContainText('123.5 km');
    await expect(page.locator('p', { hasText: 'Last synced' })).toBeVisible();
  });

  test('shows activity rows ordered by date descending', async ({ page }) => {
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
    ]);

    await page.goto('/Strava');

    await expect(page.locator('thead th')).toHaveText(['Date', 'Name', 'Type', 'Distance', 'Duration']);

    const rows = page.locator('tbody tr');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText('Morning Run');
    await expect(rows.nth(0)).toContainText('5.00 km');
    await expect(rows.nth(0)).toContainText('00:25:00');
    await expect(rows.nth(1)).toContainText('Evening Ride');
  });

  test('paginates activities at 100 per page', async ({ page }) => {
    seedAthlete({
      id: athleteId,
      firstname: 'Ada',
      lastname: 'Lovelace',
      allTimeDistanceMeters: 0,
      fetchedAtUtc: '2026-01-01 12:00:00',
    });
    seedActivities(
      Array.from({ length: 125 }, (_, i) => ({
        id: i + 1,
        athleteId,
        name: `Run ${i + 1}`,
        type: 'Run',
        distance: 5000,
        movingTime: 1500,
        startDateUtc: `2026-01-${String((i % 28) + 1).padStart(2, '0')} 06:00:00`,
        startDateLocal: `2026-01-${String((i % 28) + 1).padStart(2, '0')}T06:00:00+00:00`,
      })),
    );

    await page.goto('/Strava');

    await expect(page.locator('small', { hasText: 'Page' })).toHaveText('Page 1 of 2');
    await expect(page.locator('tbody tr')).toHaveCount(100);
    await expect(page.locator('li.page-item', { hasText: '«' })).toHaveClass(/disabled/);

    await page.locator('li.page-item', { hasText: '»' }).locator('a').click();

    await expect(page.locator('small', { hasText: 'Page' })).toHaveText('Page 2 of 2');
    await expect(page.locator('tbody tr')).toHaveCount(25);
    await expect(page.locator('li.page-item', { hasText: '»' })).toHaveClass(/disabled/);
  });

  test('shows the empty-activities state when connected but nothing synced', async ({ page }) => {
    seedAthlete({
      id: athleteId,
      firstname: 'Ada',
      lastname: 'Lovelace',
      allTimeDistanceMeters: 0,
      fetchedAtUtc: '2026-01-01 12:00:00',
    });

    await page.goto('/Strava');

    await expect(page.locator('p', { hasText: 'Activities stored' })).toContainText('0');
    await expect(page.locator('p', { hasText: 'No activities pulled yet.' })).toBeVisible();
  });
});
