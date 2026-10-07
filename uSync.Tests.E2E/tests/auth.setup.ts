import { test as setup } from '@playwright/test';
import { ConstantHelper, UiHelpers } from '@umbraco/playwright-testhelpers';
import { STORAGE_STATE } from '../playwright.config';

// falls back to the unattended install user, which is what the local test sites
// are created with.
const login =
	process.env.UMBRACO_USER_LOGIN ?? process.env.UMBRACO__CMS__UNATTENDED__UNATTENDEDUSEREMAIL;
const password =
	process.env.UMBRACO_USER_PASSWORD ??
	process.env.UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERPASSWORD;

setup('authenticate', async ({ page }) => {
	if (!login || !password) {
		throw new Error(
			'No backoffice login. Set UMBRACO_USER_LOGIN and UMBRACO_USER_PASSWORD (see .env.example).',
		);
	}

	const umbracoUi = new UiHelpers(page);

	await umbracoUi.goToBackOffice();
	await umbracoUi.login.enterEmail(login);
	await umbracoUi.login.enterPassword(password);
	await umbracoUi.login.clickLoginButton();
	await umbracoUi.login.goToSection(ConstantHelper.sections.settings);
	await page.context().storageState({ path: STORAGE_STATE });
});
