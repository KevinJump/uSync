import { ManifestLocalization } from "@umbraco-cms/backoffice/extension-registry";

const localizations: Array<ManifestLocalization> = [
    {
        type: 'localization',
        alias: 'usync.lang.enus',
        name: 'English (US)',
        weight: 0,
        meta: {
            culture: 'en-us'
        },
        js: () => import('./files/en-us')
    },
    {
        type: 'localization',
        alias: 'usync.lang.engb',
        name: 'English (GB)',
        weight: 0,
        meta: {
            culture: 'en-gb'
        },
        js: () => import('./files/en-us')
    }

]

export const manifests = [...localizations];