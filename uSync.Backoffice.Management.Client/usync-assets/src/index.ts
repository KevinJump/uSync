import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { OpenAPI } from '@jumoo/uSync';

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

	_host.consumeContext(UMB_AUTH_CONTEXT, (_auth) => {
		const umbOpenApi = _auth.getOpenApiConfiguration();
		OpenAPI.TOKEN = umbOpenApi.token;
		OpenAPI.BASE = umbOpenApi.base;
		OpenAPI.WITH_CREDENTIALS = umbOpenApi.withCredentials;
	});
};
