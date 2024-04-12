import {
    ManifestWorkspace,
    ManifestWorkspaceAction,
    ManifestWorkspaceContext,
    ManifestWorkspaceView,
} from '@umbraco-cms/backoffice/extension-registry'

import './components'

import { uSyncConstants } from '../constants.js'

const workspaceAlias = uSyncConstants.workspace.alias

const workspace: ManifestWorkspace = {
    type: 'workspace',
    alias: workspaceAlias,
    name: 'uSync core workspace',
    js: () => import('./workspace.element.js'),
    meta: {
        entityType: uSyncConstants.workspace.rootElement,
    },
}

const context: ManifestWorkspaceContext = {
    type: 'workspaceContext',
    alias: uSyncConstants.workspace.contextAlias,
    name: 'uSync workspace context',
    js: () => import('./workspace.context.js'),
}

const workspaceViews: Array<ManifestWorkspaceView> = [
    {
        type: 'workspaceView',
        alias: uSyncConstants.workspace.defaultView.alias,
        name: 'uSync workspace default view',
        js: () => import('./views/default/default.element.js'),
        weight: 300,
        meta: {
            label: 'Default',
            pathname: 'default',
            icon: 'icon-infinity',
        },
        conditions: [
            {
                alias: 'Umb.Condition.WorkspaceAlias',
                match: workspaceAlias,
            },
        ],
    },
    {
        type: 'workspaceView',
        alias: uSyncConstants.workspace.settingView.alias,
        name: 'uSync workspace settings view',
        js: () => import('./views/settings/settings.element.js'),
        weight: 200,
        meta: {
            label: 'Settings',
            pathname: 'settings',
            icon: 'icon-settings',
        },
        conditions: [
            {
                alias: 'Umb.Condition.WorkspaceAlias',
                match: workspaceAlias,
            },
        ],
    },
    {
        type: 'workspaceView',
        alias: uSyncConstants.workspace.addOnView.alias,
        name: 'uSync addons',
        js: () => import('./views/addons/addons.element.js'),
        weight: 100,
        meta: {
            label: 'AddOns',
            pathname: 'addons',
            icon: 'icon-box',
        },
        conditions: [
            {
                alias: 'Umb.Condition.WorkspaceAlias',
                match: workspaceAlias,
            },
        ],
    },
]

const workspaceActions: Array<ManifestWorkspaceAction> = []

export const manifests = [
    context,
    workspace,
    ...workspaceViews,
    ...workspaceActions,
]
