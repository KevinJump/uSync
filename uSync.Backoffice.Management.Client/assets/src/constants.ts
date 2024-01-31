
const _constants = {

    name: 'uSync',
    path: 'usync',
    icon: 'icon-infinity',
    menuName: 'Syncronisation',

    workspace: {
        alias: 'usync.workspace',
        name: 'uSync root workspace',
        rootElement: 'usync-root',
        elementName: 'usync-workspace-root',
        contextAlias: 'usync.workspace.context',

        defaultView: {
            alias: 'usync.workspace.default',
            name: 'uSync workspace default view',
            icon: 'icon-infinity',
            path: 'usync.workspace.default'
        },

        settingView: {
            alias: 'usync.workspace.settings',
            name: 'uSync workspace settings view',
            icon: 'icon-settings',
            path: 'usync.workspace.settings'
        }
    },

    dashboard: {
        name: 'uSyncDashboard',
        alias: 'usync.dashboard',
        elementName: 'usync-dashboard',
        path: 'usync.dashboard',
        weight: -10,
        section: 'Umb.Section.Settings',
    },

    tree : {
        name: 'uSyncTree',
        alias: 'usync.tree',
        respository: ''
    },

    menu : {
        sidebar: 'usync.sidebar',
        alias: 'usync.menu',
        name: 'usync.Menu',
        label: 'Syncronisation',

        item: {
            alias: 'usync.menu.item',
            name: 'usync.menu.item'
        }
    }

};


export const uSyncConstants = _constants;