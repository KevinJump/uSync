
const _constants = {

    name: 'uSync',
    path: 'usync',
    icon: 'icon-infinity',
    menuName: 'Syncronisation',

    workspace: {
        alias: 'usync.workspace',
        rootElement: 'usync-root',
        elementName: 'usync-workspace-root',
        contextAlias: 'usync.workspace.context',

        defaultView: {
            alias: 'usync.workspace.default',
            path: 'usync.workspace.default'
        },

        settingView: {
            alias: 'usync.workspace.settings',
            path: 'usync.workspace.settings'
        }
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