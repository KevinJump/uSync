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
];

export const manifests: UmbExtensionManifest[] = [...localizations];
