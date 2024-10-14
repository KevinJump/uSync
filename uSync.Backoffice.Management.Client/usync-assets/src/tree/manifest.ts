import { uSyncMenuElement } from '@jumoo/uSync';

const sectionAlias = 'Umb.Section.Settings';

const menuConstants = {
	alias: 'usync.menu',
	name: 'uSync',
	icon: 'icon-infinity',
	rootElement: 'usync-root',
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
		entityType: 'usync-root',
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
