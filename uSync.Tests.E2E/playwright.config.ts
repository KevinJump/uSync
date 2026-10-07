import { defineConfig, devices } from '@playwright/test';
import { existsSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';
import 'dotenv/config';

const root = dirname(fileURLToPath(import.meta.url));

const defaultUrl = 'https://localhost:44398';
const baseURL = process.env.UMBRACO_URL ?? defaultUrl;

export const STORAGE_STATE = join(root, '.auth/user.json');

// the testhelpers read the auth tokens from this file, and the site url from URL.
process.env.STORAGE_STAGE_PATH = STORAGE_STATE;
process.env.URL = baseURL;

// the local test sites are gitignored, so only start one when it is there, and
// when we haven't been pointed at some other site.
const site = join(root, '../uSyncSource.Site');
const startSite = !process.env.UMBRACO_URL && existsSync(site);

export default defineConfig({
	testDir: './tests',
	timeout: 60 * 1000,
	expect: { timeout: 15 * 1000 },
	// the tests share one site (and one uSync folder), so they can't run side by side.
	fullyParallel: false,
	workers: 1,
	forbidOnly: !!process.env.CI,
	retries: process.env.CI ? 2 : 0,
	reporter: process.env.CI ? 'line' : [['list'], ['html', { open: 'never' }]],
	use: {
		baseURL,
		trace: 'retain-on-failure',
		screenshot: 'only-on-failure',
		ignoreHTTPSErrors: true,
		// Umbraco uses 'data-mark' not 'data-testid'
		testIdAttribute: 'data-mark',
	},
	projects: [
		{
			name: 'setup',
			testMatch: '**/*.setup.ts',
		},
		{
			name: 'e2e',
			testMatch: '**/*.spec.ts',
			dependencies: ['setup'],
			use: {
				...devices['Desktop Chrome'],
				storageState: STORAGE_STATE,
			},
		},
	],
	webServer: startSite
		? {
				command: 'dotnet run --project "' + site + '" --launch-profile Umbraco.Web.UI',
				url: defaultUrl + '/umbraco',
				ignoreHTTPSErrors: true,
				reuseExistingServer: true,
				timeout: 5 * 60 * 1000,
				stdout: 'ignore',
				stderr: 'pipe',
			}
		: undefined,
});
