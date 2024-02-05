// load up the things here. 
import { ManifestTypes } from '@umbraco-cms/backoffice/extension-registry';

import { manifests as trees } from './tree/manifest';
import { manifests as workspace } from './workspace/manifest';
import { manifests as signalr } from './signalr/manifest';
import { manifests as localization } from './lang/manifest';

const contexts: Array<ManifestTypes> = [
    {
        type: 'globalContext',
        alias: 'uSync.GlobalContext.Actions',
        name: 'uSync Action Context',
        js: () => import('./workspace/workspace.context.js')        
    }
]

export const manifests = [
    ...localization,
    ...trees,
    ...workspace,
    ...signalr,
    ...contexts,
]