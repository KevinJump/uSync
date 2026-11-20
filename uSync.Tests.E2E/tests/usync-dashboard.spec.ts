import { expect } from '@playwright/test';
import { test } from '@umbraco/playwright-testhelpers';

test.describe('uSync dashboard', () => {
	test.beforeEach(async ({ umbracoUi }) => {
		await umbracoUi.goToBackOffice();
		await umbracoUi.content.goToSection('Synchronisation');
		await umbracoUi.page.getByRole('link', { name: 'uSync', exact: true }).click();
	});

	test('opens from the synchronisation section', { tag: '@smoke' }, async ({ umbracoUi }) => {
		const page = umbracoUi.page;

		await expect(page.locator('usync-workspace-root')).toBeVisible();
		await expect(page.locator('usync-default-view')).toBeVisible();
		for (const view of ['default', 'settings', 'addons']) {
			await expect(page.getByTestId('workspace:view-link:usync.workspace.' + view)).toBeVisible();
		}
	});

	test('shows the actions for each group', { tag: '@smoke' }, async ({ umbracoUi }) => {
		const settings = umbracoUi.page
			.locator('usync-action-box')
			.filter({ has: umbracoUi.page.getByRole('heading', { name: 'Settings' }) });

		await expect(settings).toBeVisible();
		// actions with options render as a button group, so find them by role, not by the uui-button class.
		for (const action of ['Report', 'Import', 'Export']) {
			await expect(settings.getByRole('button', { name: action, exact: true }).first()).toBeVisible();
		}
	});

	test('can run a report', async ({ umbracoUi }) => {
		test.setTimeout(3 * 60 * 1000);

		const page = umbracoUi.page;
		const settings = page
			.locator('usync-action-box')
			.filter({ has: page.getByRole('heading', { name: 'Settings' }) });

		await settings.getByRole('button', { name: 'Report', exact: true }).first().click();

		await expect(page.locator('usync-progress-box')).toBeVisible();
		await expect(page.locator('usync-results')).toBeVisible({ timeout: 2 * 60 * 1000 });
	});

	test('can search the report results', async ({ umbracoUi }) => {
		test.setTimeout(3 * 60 * 1000);

		const page = umbracoUi.page;
		const settings = page
			.locator('usync-action-box')
			.filter({ has: page.getByRole('heading', { name: 'Settings' }) });

		await settings.locator('uui-button.action-button[label="Report"]').click();

		const results = page.locator('usync-results');
		await expect(results).toBeVisible({ timeout: 2 * 60 * 1000 });

		await results.locator('uui-toggle').click();
		await results.locator('uui-button[label="Filter results"]').click();

		const search = results.locator('uui-input').locator('input');
		const noMatch = results.getByText('No items match the current filters');

		await search.fill('zz-no-such-item-zz');
		await expect(noMatch).toBeVisible();

		await search.fill('');
		await expect(noMatch).toBeHidden();
	});
});
