import { expect, type Page } from '@playwright/test';
import { ConstantHelper, test } from '@umbraco/playwright-testhelpers';
import { existsSync, rmSync, writeFileSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';

// the preview tests need their own items in the site's uSync folder, so they only
// run against the local uSyncSource.Site (not a site set with UMBRACO_URL).
const root = dirname(fileURLToPath(import.meta.url));
const syncFolder = join(root, '../../uSyncSource.Site/uSync/v17');
const canWriteFixtures = !process.env.UMBRACO_URL && existsSync(syncFolder);

const dataTypeKey = 'e2e0f1a2-b3c4-4d5e-8f60-718293a4b5c6';
const contentTypeKey = 'e2e1a2b3-c4d5-4e6f-8071-8293a4b5c6d7';

const fixtures = {
	'DataTypes/E2EPreviewText.config': `<?xml version="1.0" encoding="utf-8"?>
<DataType Key="${dataTypeKey}" Alias="E2E Preview Text" Level="1">
  <Info>
    <Name>E2E Preview Text</Name>
    <EditorAlias>Umbraco.TextBox</EditorAlias>
    <EditorUIAlias>Umb.PropertyEditorUi.TextBox</EditorUIAlias>
  </Info>
  <Config><![CDATA[{ "maxChars": 42 }]]></Config>
</DataType>
`,
	'ContentTypes/e2epreviewpage.config': `<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="${contentTypeKey}" Alias="e2ePreviewPage" Level="1">
  <Info>
    <Name>E2E Preview Page</Name>
    <Icon>icon-document</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description></Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>false</IsElement>
    <Compositions />
    <DefaultTemplate></DefaultTemplate>
    <AllowedTemplates />
  </Info>
  <Structure />
  <GenericProperties>
    <GenericProperty>
      <Key>e2e2b3c4-d5e6-4f70-8192-a3b4c5d6e7f8</Key>
      <Name>E2E Title</Name>
      <Alias>e2eTitle</Alias>
      <Definition>${dataTypeKey}</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>0</SortOrder>
      <Tab Alias="content/details">Details</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
  </GenericProperties>
  <Tabs>
    <Tab>
      <Key>e2e3c4d5-e6f7-4081-92a3-b4c5d6e7f809</Key>
      <Caption>Content</Caption>
      <Alias>content</Alias>
      <Type>Tab</Type>
      <SortOrder>0</SortOrder>
    </Tab>
    <Tab>
      <Key>e2e4d5e6-f708-4192-a3b4-c5d6e7f8091a</Key>
      <Caption>Details</Caption>
      <Alias>content/details</Alias>
      <Type>Group</Type>
      <SortOrder>0</SortOrder>
    </Tab>
  </Tabs>
</ContentType>
`,
};

async function openPreview(page: Page, group: string, item: string) {
	const groupView = page.locator('usync-result-group').filter({ hasText: group });
	await groupView.locator('.summary').click();
	await groupView.locator('usync-result-row').filter({ hasText: item }).locator('.row').click();

	const modal = page.locator('usync-details-modal');
	await modal.locator('uui-tab').filter({ hasText: 'Preview' }).click();
	return modal;
}

test.describe('uSync item preview', () => {
	test.skip(!canWriteFixtures, 'needs the local uSyncSource.Site uSync folder');

	test.beforeAll(() => {
		for (const [file, content] of Object.entries(fixtures)) {
			writeFileSync(join(syncFolder, file), content);
		}
	});

	test.afterAll(() => {
		for (const file of Object.keys(fixtures)) {
			rmSync(join(syncFolder, file), { force: true });
		}
	});

	test.beforeEach(async ({ umbracoUi }) => {
		test.setTimeout(3 * 60 * 1000);

		await umbracoUi.goToBackOffice();
		await umbracoUi.content.goToSection(ConstantHelper.sections.settings);
		await umbracoUi.page.getByRole('link', { name: 'uSync', exact: true }).click();

		const settings = umbracoUi.page
			.locator('usync-action-box')
			.filter({ has: umbracoUi.page.getByRole('heading', { name: 'Settings' }) });
		await settings.locator('uui-button.action-button[label="Report"]').click();
		await expect(umbracoUi.page.locator('usync-results')).toBeVisible({ timeout: 2 * 60 * 1000 });
	});

	test('previews a new data type with its editor', async ({ umbracoUi }) => {
		const modal = await openPreview(umbracoUi.page, 'DataTypes', 'E2E Preview Text');
		const preview = modal.locator('usync-data-type-preview');

		await expect(preview).toBeVisible();
		await expect(preview).toContainText('This item does not exist in the site yet');
		await expect(preview.locator('umb-property umb-property-editor-ui-text-box')).toBeVisible();
		await expect(preview).toContainText('maxChars');
	});

	test('previews a new content type that uses a new data type', async ({ umbracoUi }) => {
		const modal = await openPreview(umbracoUi.page, 'Content Types', 'e2ePreviewPage');
		const preview = modal.locator('usync-content-type-preview');

		await expect(preview).toBeVisible();
		await expect(preview).toContainText('E2E Preview Page');
		await expect(preview).toContainText('Details');

		// the property's data type only exists in the uSync files, so the preview builds it from there.
		const property = preview.locator('.property').filter({ hasText: 'e2eTitle' });
		await expect(property).toContainText('E2E Preview Text');
		await expect(property).toContainText('new data type');
		await expect(property.locator('umb-property-editor-ui-text-box')).toBeVisible();
	});
});
