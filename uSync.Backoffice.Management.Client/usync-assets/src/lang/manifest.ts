const localizations: Array<UmbExtensionManifest> = [
	{
		type: 'localization',
		alias: 'usync.lang.enus',
		name: 'English',
		weight: 0,
		meta: {
			culture: 'en',
		},
		js: () => import('./files/en-us'),
	},
	{
		type: 'localization',
		alias: 'usync.lang.dadk',
		name: 'Danish',
		weight: 0,
		meta: {
			culture: 'da',
		},
		js: () => import('./files/da-dk'),
	},
	{
		type: 'localization',
		alias: 'usync.lang.frfr',
		name: 'French',
		weight: 0,
		meta: {
			culture: 'fr',
		},
		js: () => import('./files/fr-fr'),
	},
	{
		type: 'localization',
		alias: 'usync.lang.eses',
		name: 'Spanish',
		weight: 0,
		meta: {
			culture: 'es',
		},
		js: () => import('./files/es-es'),
	},
	{
		type: 'localization',
		alias: 'usync.lang.dede',
		name: 'German',
		weight: 0,
		meta: {
			culture: 'de',
		},
		js: () => import('./files/de-de'),
	},
	{
		type: 'localization',
		alias: 'usync.lang.nlnl',
		name: 'Dutch',
		weight: 0,
		meta: {
			culture: 'nl',
		},
		js: () => import('./files/nl-nl'),
	},
];

export const manifests: UmbExtensionManifest[] = [...localizations];
