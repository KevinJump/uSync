import { ManifestIcons } from '@umbraco-cms/backoffice/extension-registry';

const icons: ManifestIcons = {
	type: 'icons',
	alias: 'usync.icons',
	name: 'uSync Icons',
	js: () => import('./icons.js'),
};

export const manifests = [icons];
