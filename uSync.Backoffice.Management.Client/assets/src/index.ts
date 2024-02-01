import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';

import { manifests } from './manifests.js';
import "./components";

export * from './repository/index.js';

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {

    // register the manifests
    extensionRegistry.registerMany(manifests);
};