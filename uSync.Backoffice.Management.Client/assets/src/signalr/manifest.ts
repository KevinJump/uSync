import { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";


const manifest: ManifestGlobalContext = {
    type: 'globalContext',
    alias: 'usync-signalr',
    name: 'usync signalr context',
    js: () => import('./signalr.context')
};

export const manifests = [manifest];