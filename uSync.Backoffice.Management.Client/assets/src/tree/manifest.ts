import { ManifestMenu, ManifestMenuItem, ManifestTree, ManifestTypes } from "@umbraco-cms/backoffice/extension-registry";
import { uSyncConstants } from "../constants";

const sectionAlias = 'Umb.Section.Settings';

const menu : ManifestMenu = {
    type: 'menu',
    alias: uSyncConstants.menu.alias,
    name: uSyncConstants.menu.name,
    meta: {
        label: uSyncConstants.menu.label
    }
}

const menuSidebarApp: ManifestTypes = {
    type: 'sectionSidebarApp',
    kind: 'menu',
    alias: uSyncConstants.menu.sidebar,
    name: 'uSync section sidebar menu',
    weight: 150,
    meta: {
        label: uSyncConstants.menu.label,
        menu: uSyncConstants.menu.alias,
    },
    conditions: [
        {
            alias: 'Umb.Condition.SectionAlias',
            match: sectionAlias,
        }
    ],
}

const tree : ManifestTree = {
    type: "tree",
    alias: uSyncConstants.tree.alias,
    name: uSyncConstants.tree.name,
    meta: {
        repositoryAlias: uSyncConstants.tree.respository
    }
};

const menuItem : ManifestMenuItem = {
    type: "menuItem",
    alias: uSyncConstants.menu.item.alias,
    name: uSyncConstants.menu.item.name,
    meta: {
        label: uSyncConstants.name,
        icon: uSyncConstants.icon,
        entityType: uSyncConstants.workspace.rootElement,
        menus: [uSyncConstants.menu.alias]
    }
}

export const manifests = [menu, menuSidebarApp, menuItem, tree];