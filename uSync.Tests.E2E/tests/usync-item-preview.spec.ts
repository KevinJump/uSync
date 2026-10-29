import { expect, type Page } from '@playwright/test';
import { ConstantHelper, test } from '@umbraco/playwright-testhelpers';
import { existsSync, readFileSync, rmSync, writeFileSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';

// the preview tests need their own items in the site's uSync folder, so they only
// run against the local uSyncSource.Site (not a site set with UMBRACO_URL).
const root = dirname(fileURLToPath(import.meta.url));
const syncFolder = join(root, '../../uSyncSource.Site/uSync/v17');
const canWriteFixtures = !process.env.UMBRACO_URL && existsSync(syncFolder);

const dataTypeKey = 'e2e0f1a2-b3c4-4d5e-8f60-718293a4b5c6';
const contentTypeKey = 'e2e1a2b3-c4d5-4e6f-8071-8293a4b5c6d7';
const compositionKey = 'e2e5e6f7-0819-42a3-b4c5-d6e7f8091a2b';

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
    <Compositions>
      <Composition Key="${compositionKey}">e2ePreviewComposition</Composition>
    </Compositions>
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
	'ContentTypes/e2epreviewcomposition.config': `<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="${compositionKey}" Alias="e2ePreviewComposition" Level="1">
  <Info>
    <Name>E2E Preview Composition</Name>
    <Icon>icon-science</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description></Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>true</IsElement>
    <Compositions />
    <DefaultTemplate></DefaultTemplate>
    <AllowedTemplates />
  </Info>
  <Structure />
  <GenericProperties>
    <GenericProperty>
      <Key>e2e6f708-192a-43b4-c5d6-e7f8091a2b3c</Key>
      <Name>E2E Summary</Name>
      <Alias>e2eSummary</Alias>
      <Definition>${dataTypeKey}</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>1</SortOrder>
      <Tab Alias="content/details">Details</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
  </GenericProperties>
  <Tabs>
    <Tab>
      <Key>e2e7081a-2a3b-44c5-d6e7-f8091a2b3c4d</Key>
      <Caption>Content</Caption>
      <Alias>content</Alias>
      <Type>Tab</Type>
      <SortOrder>0</SortOrder>
    </Tab>
    <Tab>
      <Key>e2e8192a-3b4c-45d6-e7f8-091a2b3c4d5e</Key>
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

	test('previews a new content type with a new composition', async ({ umbracoUi }) => {
		const modal = await openPreview(umbracoUi.page, 'Content Types', 'e2ePreviewPage');
		const preview = modal.locator('usync-content-type-preview');

		// the composition only exists in the uSync files, its group merges into the page's.
		await expect(preview).toContainText('E2E Preview Composition (new)');
		const inherited = preview.locator('.property').filter({ hasText: 'e2eSummary' });
		await expect(inherited).toContainText('from E2E Preview Composition');
		await expect(inherited.locator('umb-property-editor-ui-text-box')).toBeVisible();
	});
});

const newPageKey = 'e2e9a2b3-4c5d-46e7-8f80-91a2b3c4d5e6';

test.describe('uSync content preview', () => {
	test.skip(!canWriteFixtures, 'needs the local uSyncSource.Site uSync folder');

	// the about page is changed in place (and put back afterwards), the new page is a copy of contact.
	const aboutFile = join(syncFolder, 'Content/about.config');
	const newPageFile = join(syncFolder, 'Content/e2e-preview-page.config');
	let about = '';

	test.beforeAll(() => {
		about = readFileSync(aboutFile, 'utf-8');
		writeFileSync(
			aboutFile,
			about
				.replace('<![CDATA[All about this starter kit]]>', '<![CDATA[E2E preview subtitle]]>')
				.replace('<SortOrder>1</SortOrder>', '<SortOrder>7</SortOrder>'),
		);

		const contact = readFileSync(join(syncFolder, 'Content/contact.config'), 'utf-8');
		writeFileSync(
			newPageFile,
			contact
				.replace(/<Content Key="[^"]+" Alias="[^"]+"/, `<Content Key="${newPageKey}" Alias="E2E Preview Content"`)
				.replace(/<NodeName Default="[^"]+"/, '<NodeName Default="E2E Preview Content"'),
		);
	});

	test.afterAll(() => {
		if (about) writeFileSync(aboutFile, about);
		rmSync(newPageFile, { force: true });
	});

	test.beforeEach(async ({ umbracoUi }) => {
		test.setTimeout(3 * 60 * 1000);

		await umbracoUi.goToBackOffice();
		await umbracoUi.content.goToSection(ConstantHelper.sections.settings);
		await umbracoUi.page.getByRole('link', { name: 'uSync', exact: true }).click();

		const content = umbracoUi.page
			.locator('usync-action-box')
			.filter({ has: umbracoUi.page.getByRole('heading', { name: 'Content', exact: true }) });
		await content.locator('uui-button.action-button[label="Report"]').click();
		await expect(umbracoUi.page.locator('usync-results')).toBeVisible({ timeout: 2 * 60 * 1000 });
	});

	test('previews changed values with the real editors', async ({ umbracoUi }) => {
		const modal = await openPreview(umbracoUi.page, 'Content', 'About');
		const preview = modal.locator('usync-content-preview');
		const [current, incoming] = [preview.locator('.side').first(), preview.locator('.side').last()];

		await expect(preview).toBeVisible();
		await expect(current.locator('.meta')).toContainText('1');
		await expect(incoming.locator('.meta')).toContainText('7');

		// opens on the tab with the change, and marks the property that changes.
		const changed = incoming.locator('.property.changed');
		await expect(changed).toContainText('Subtitle');
		await expect(changed.locator('umb-property-editor-ui-text-box input')).toHaveValue('E2E preview subtitle');
		await expect(current.locator('.property.changed umb-property-editor-ui-text-box input')).toHaveValue(
			'All about this starter kit',
		);

		// only show changes hides the properties that are the same.
		await preview.locator('uui-toggle').click();
		await expect(incoming.locator('.property')).toHaveCount(1);
	});

	test('previews a new page', async ({ umbracoUi }) => {
		const modal = await openPreview(umbracoUi.page, 'Content', 'E2E Preview Content');
		const preview = modal.locator('usync-content-preview');

		await expect(preview).toBeVisible();
		await expect(preview).toContainText('This item does not exist in the site yet');
		await expect(preview.locator('.side').last()).toContainText('E2E Preview Content');
		await expect(preview.locator('umb-property').first()).toBeVisible();
	});
});
