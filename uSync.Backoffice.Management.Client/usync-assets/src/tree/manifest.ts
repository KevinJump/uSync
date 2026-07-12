import { uSyncConstants, uSyncMenuElement } from '@jumoo/uSync';
import { USYNC_CONDITION_NEW_SECTION } from '../conditions/constants';
import { USYNC_SECTION_ALIAS } from './constants';

const sectionAlias = 'Umb.Section.Settings';

/*
 * config slight of hand: This manifest is added in c#
 * via a manaifestreader, that only adds it if a setting
 * is true in the appsettings.json file.
 *
 * we can then use conditional code that will only allow
 * the tree to show in settings when this section doesn't
 * exist globally.
 */
/*
const uSyncSection: UmbExtensionManifest = {
	type: 'section',
	alias: USYNC_SECTION_ALIAS,
	name: 'uSync',
	weight: 350,
	meta: {
		label: '#uSync_section',
		pathname: 'sync',
	},
};
*/

const menuConstants = {
	alias: 'usync.menu',
	name: 'uSync',
	icon: 'icon-infinity',
	rootElement: uSyncConstants.workspace.rootElement,
};

const menu: UmbExtensionManifest = {
	type: 'menu',
	alias: menuConstants.alias,
	name: menuConstants.name,
	meta: {
		label: menuConstants.name,
		icon: menuConstants.icon,
		entityType: menuConstants.rootElement,
	},
};

const menuItem: UmbExtensionManifest = {
	type: 'menuItem',
	alias: 'usync.menu.item',
	name: 'uSync menu item',
	element: uSyncMenuElement,
	meta: {
		label: 'uSync',
		icon: 'usync-logo',
		entityType: uSyncConstants.workspace.rootElement,
		menus: [menuConstants.alias],
	},
};

const menuSidebarApp: UmbExtensionManifest = {
	type: 'sectionSidebarApp',
	kind: 'menu',
	alias: 'usync.sidebarapp',
	name: 'uSync section sidebar menu',
	weight: 150,
	meta: {
		label: '#uSync_section',
		menu: menu.alias,
	},
	conditions: [
		{
			alias: 'Umb.Condition.SectionAlias',
			oneOf: [sectionAlias, USYNC_SECTION_ALIAS],
		},
		{
			alias: USYNC_CONDITION_NEW_SECTION,
		},
	],
};

/// example of how to extend uSync menus.

// const subMenuItem : ManifestuSyncMenuItem = {
//     type: 'usync-menuItem',
//     alias: 'usync.menu.sub.item',
//     name: 'uSync core menu item',
//     meta: {
//         label: 'uSync Extension',
//         icon: 'icon-brick',
//         entityType: 'usync-root',
//         menus: [uSyncConstants.menuAlias],
//     }
// }

export const manifests = [
	// uSyncSection,
	menu,
	menuSidebarApp,
	menuItem,
	// subMenuItem
];
