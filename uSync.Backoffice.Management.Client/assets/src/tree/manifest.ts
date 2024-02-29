import { ManifestMenu, ManifestTypes } from "@umbraco-cms/backoffice/extension-registry";
import { uSyncConstants } from "../constants";
// import { ManifestuSyncMenuItem } from "./types.js";

const sectionAlias = 'Umb.Section.Settings';

const menu : ManifestMenu = {
    type: 'menu',
    alias: 'usync.menu',
    name: 'uSync Menu',
    element: () => import('./usync.menu-element.js'),
    meta: {
        label: uSyncConstants.name,
        icon: uSyncConstants.icon,
        entityType: uSyncConstants.workspace.rootElement
    }
}

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
        }
    ],
}

/// example of how to extend uSync menus.

// const menuItem : ManifestuSyncMenuItem = {
//     type: "usync-menuItem",
//     alias: 'usync.menu.item',
//     name: 'uSync core menu item',
//     meta: {
//         label: 'uSync Extension',
//         icon: 'icon-brick',
//         entityType: uSyncConstants.workspace.rootElement,
//         menus: [menu.alias]
//     }
// }

export const manifests = [
    menu, 
    menuSidebarApp, 
    // menuItem
];