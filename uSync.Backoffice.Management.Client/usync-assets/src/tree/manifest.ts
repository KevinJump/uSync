import {
	ManifestMenu,
	ManifestMenuItem,
	ManifestTypes,
} from '@umbraco-cms/backoffice/extension-registry';
import { uSyncConstants } from '../constants.js';
import uSyncMenuElement from './usync.menu-element.js';

const sectionAlias = 'Umb.Section.Settings';

const menu: ManifestMenu = {
	type: 'menu',
	alias: uSyncConstants.menuAlias,
	name: 'uSync Menu',
	meta: {
		label: uSyncConstants.name,
		icon: uSyncConstants.icon,
		entityType: uSyncConstants.workspace.rootElement,
	},
};

const menuItem: ManifestMenuItem = {
	type: 'menuItem',
	alias: 'usync.menu.item',
	name: 'uSync menu item',
	element: uSyncMenuElement,
	meta: {
		label: 'uSync',
		icon: 'icon-infinity',
		entityType: 'usync-root',
		menus: [uSyncConstants.menuAlias],
	},
};

const menuSidebarApp: ManifestTypes = {
	type: 'sectionSidebarApp',
	kind: 'menu',
	alias: 'usync.sidebarapp',
	name: 'uSync section sidebar menu',
	weight: 150,
	meta: {
		label: 'Syncronisation',
		menu: menu.alias,
	},
	conditions: [
		{
			alias: 'Umb.Condition.SectionAlias',
			match: sectionAlias,
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
	menu,
	menuSidebarApp,
	menuItem,
	// subMenuItem
];
