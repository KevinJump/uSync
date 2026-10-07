import { expect } from '@playwright/test';
import { ConstantHelper, test } from '@umbraco/playwright-testhelpers';

test.describe('uSync dashboard', () => {
	test.beforeEach(async ({ umbracoUi }) => {
		await umbracoUi.goToBackOffice();
		await umbracoUi.content.goToSection(ConstantHelper.sections.settings);
		await umbracoUi.page.getByRole('link', { name: 'uSync', exact: true }).click();
	});

	test('opens from the settings menu', { tag: '@smoke' }, async ({ umbracoUi }) => {
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
		for (const action of ['Report', 'Import', 'Export']) {
			await expect(settings.locator('uui-button.action-button[label="' + action + '"]')).toBeVisible();
		}
	});

	test('can run a report', async ({ umbracoUi }) => {
		test.setTimeout(3 * 60 * 1000);

		const page = umbracoUi.page;
		const settings = page
			.locator('usync-action-box')
			.filter({ has: page.getByRole('heading', { name: 'Settings' }) });

		await settings.locator('uui-button.action-button[label="Report"]').click();

		await expect(page.locator('usync-progress-box')).toBeVisible();
		await expect(page.locator('usync-results')).toBeVisible({ timeout: 2 * 60 * 1000 });
	});
});
