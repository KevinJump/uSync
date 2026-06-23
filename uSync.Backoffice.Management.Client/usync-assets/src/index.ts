import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';

import './components/index.js';
import './dialogs/index.js';
import './signalr/index.js';

import './external/signalr/index.js';
import './api/index.js';

export * from './exports.js';

import { manifests } from './manifests.js';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { client } from './api/client.gen.js';

export const onInit: UmbEntryPointOnInit = async (host, extensionRegistry) => {
	// register the manifests
	extensionRegistry.registerMany(manifests);

	const authContext = await host.getContext(UMB_AUTH_CONTEXT);
	if (!authContext) {
		console.warn(
			'UMB_AUTH_CONTEXT not available — extension API client will not be authenticated',
		);
		return;
	}
	authContext.configureClient(client);
};
