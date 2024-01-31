import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';

export * from './repository/index.js';

// load up the things here. 
import { manifests as trees } from './tree/manifest';
import { manifests as workspace } from './workspace/manifest';
import { ManifestTypes } from '@umbraco-cms/backoffice/extension-registry';

import "./components";

const contexts: Array<ManifestTypes> = [
    {
        type: 'globalContext',
        alias: 'uSync.GlobalContext.Actions',
        name: 'uSync Action Context',
        js: () => import('./workspace/workspace.context.js')
        
    }
]

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
    console.log(workspace);

    // register them here. 
    extensionRegistry.registerMany(
        [
            ...contexts,
            ...trees,
            ...workspace
        ]);
};