import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';

import './components/index.js';
import './dialogs/index.js';
import './signalr/index.js';

import './external/signalr/index.js';
import './api/index.js';

export * from './exports.js';

import { manifests } from './manifests.js';

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
	// register the manifests
	extensionRegistry.registerMany(manifests);
};
