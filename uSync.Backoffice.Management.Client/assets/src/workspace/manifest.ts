import { 
    ManifestWorkspace, 
    ManifestWorkspaceAction, 
    ManifestWorkspaceContext, 
    ManifestWorkspaceView 
} from "@umbraco-cms/backoffice/extension-registry";
import { uSyncConstants } from "../constants.js";

const workspaceAlias = uSyncConstants.workspace.alias;

const context: ManifestWorkspaceContext = {
    type: 'workspaceContext',
    alias: uSyncConstants.workspace.contextAlias,
    name: 'uSync workspace context',
    js: () => import('./workspace.context.js'),
}


const workspace: ManifestWorkspace = {
    type: 'workspace',
    alias: workspaceAlias,
    name: uSyncConstants.workspace.name,
    js: () => import('./workspace.element.js'),
    meta: {
        entityType: uSyncConstants.workspace.rootElement,
    }
};

/**
 * this isn't working, don't know why :( - going to go hardwired for now
 */
const workspaceViews: Array<ManifestWorkspaceView> = [
    {
        type: 'workspaceView',
        alias: uSyncConstants.workspace.defaultView.alias,
        name: uSyncConstants.workspace.defaultView.name,
        js: () => import('./views/default/default.element.js'),
        weight: 300,
        meta: {
            label: 'Default',
            pathname: 'default',
            icon: 'icon-box',
        },
        conditions: [
            {
				alias: 'Umb.Condition.WorkspaceAlias',
                match: workspaceAlias
            }
        ]
    },
    {
        type: 'workspaceView',
        alias: uSyncConstants.workspace.settingView.alias,
        name: uSyncConstants.workspace.settingView.name,
        js: () => import('./views/settings/settings.element.js'),
        weight: 200,
        meta: {
            label: 'Settings',
            pathname: 'settings',
            icon: 'icon-box'
        },
        conditions: [
            {
				alias: 'Umb.Condition.WorkspaceAlias',
                match: workspaceAlias,
            }
        ]       
    }
];

const workspaceActions: Array<ManifestWorkspaceAction> = [];

export const manifests = [context, workspace, ...workspaceViews, ...workspaceActions];