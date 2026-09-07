import { test, expect, APIRequestContext } from '@playwright/test';

const API_URL = 'http://localhost:5132';

const seedProducts = [
  { name: 'Apple Basket', price: 10.0 },
  { name: 'Banana Bunch', price: 3.5 },
  { name: 'Cherry Box', price: 1.25 },
];

async function createProduct(request: APIRequestContext, product: { name: string; price: number }) {
  const response = await request.post(`${API_URL}/api/products`, { data: product });
  return (await response.json()) as { id: number; name: string; price: number };
}

async function deleteProduct(request: APIRequestContext, id: number) {
  await request.delete(`${API_URL}/api/products/${id}`);
}

test.describe('Products page sorting', () => {
  let seededIds: number[] = [];

  test.beforeAll(async ({ playwright }) => {
    const request = await playwright.request.newContext();
    seededIds = [];
    for (const product of seedProducts) {
      const created = await createProduct(request, product);
      seededIds.push(created.id);
    }
    await request.dispose();
  });

  test.afterAll(async ({ playwright }) => {
    const request = await playwright.request.newContext();
    for (const id of seededIds) {
      await deleteProduct(request, id);
    }
    await request.dispose();
  });

  async function seededTitlesInOrder(page: import('@playwright/test').Page) {
    const titles = await page.locator('.card-title').allTextContents();
    const seededNames = new Set(seedProducts.map((p) => p.name));
    return titles.map((t) => t.trim()).filter((t) => seededNames.has(t));
  }

  test('sorts by name ascending and descending', async ({ page }) => {
    await page.goto('/Products');

    await page.getByRole('link', { name: 'Name', exact: true }).click();
    await expect(page).toHaveURL(/sortBy=name/);
    expect(await seededTitlesInOrder(page)).toEqual(['Apple Basket', 'Banana Bunch', 'Cherry Box']);

    await page.getByRole('link', { name: /^Name/ }).click();
    await expect(page).toHaveURL(/descending=True/);
    expect(await seededTitlesInOrder(page)).toEqual(['Cherry Box', 'Banana Bunch', 'Apple Basket']);
  });

  test('sorts by price ascending and descending', async ({ page }) => {
    await page.goto('/Products');

    await page.getByRole('link', { name: 'Price', exact: true }).click();
    await expect(page).toHaveURL(/sortBy=price/);
    expect(await seededTitlesInOrder(page)).toEqual(['Cherry Box', 'Banana Bunch', 'Apple Basket']);

    await page.getByRole('link', { name: /^Price/ }).click();
    await expect(page).toHaveURL(/descending=True/);
    expect(await seededTitlesInOrder(page)).toEqual(['Apple Basket', 'Banana Bunch', 'Cherry Box']);
  });

  test('highlights the active sort field', async ({ page }) => {
    await page.goto('/Products?sortBy=price&descending=false');

    const priceButton = page.getByRole('link', { name: /^Price/ });
    const nameButton = page.getByRole('link', { name: 'Name', exact: true });

    await expect(priceButton).toHaveClass(/btn-primary/);
    await expect(nameButton).not.toHaveClass(/btn-primary/);
  });
});
