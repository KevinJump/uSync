const icons: UmbExtensionManifest = {
	type: 'icons',
	alias: 'usync.icons',
	name: 'uSync Icons',
	js: () => import('./icons.js'),
};

export const manifests = [icons];
