import { ManifestMenu, ManifestMenuItem, ManifestTypes } from "@umbraco-cms/backoffice/extension-registry";
import { uSyncConstants } from "../constants";

const sectionAlias = 'Umb.Section.Settings';

const menu : ManifestMenu = {
    type: 'menu',
    alias: 'usync.menu',
    name: 'uSync Menu',
    meta: {
        label: 'Syncronisation'
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

const menuItem : ManifestMenuItem = {
    type: "menuItem",
    alias: 'usync.menu.item',
    name: 'uSync core menu item',
    meta: {
        label: uSyncConstants.name,
        icon: uSyncConstants.icon,
        entityType: uSyncConstants.workspace.rootElement,
        menus: [menu.alias]
    }
}

export const manifests = [menu, menuSidebarApp, menuItem];