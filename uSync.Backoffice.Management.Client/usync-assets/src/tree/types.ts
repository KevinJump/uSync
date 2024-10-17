import { ManifestElement } from '@umbraco-cms/backoffice/extension-api';
import { MetaMenuItem, UmbMenuItemElement } from '@umbraco-cms/backoffice/menu';

/**
 * Defines a sub menu item extension for the uSync menu
 */
export interface ManifestuSyncMenuItem extends ManifestElement<UmbMenuItemElement> {
	type: 'usync-menuItem';
	meta: MetaMenuItem;
}

declare global {
	interface UmbExtensionManifestMap {
		ManifestuSyncMenuItem: ManifestuSyncMenuItem;
	}
}
