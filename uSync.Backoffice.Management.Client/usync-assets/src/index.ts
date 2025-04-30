import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

import './components/index.js';
import './dialogs/index.js';
import './signalr/index.js';

import './external/signalr/index.js';
import './api/index.js';

export * from './exports.js';

import { manifests } from './manifests.js';
import { client } from './api/client.gen.js';

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
	// register the manifests
	extensionRegistry.registerMany(manifests);

	_host.consumeContext(UMB_AUTH_CONTEXT, (_auth) => {
		if (!_auth) return;

		var config = _auth.getOpenApiConfiguration();

		client.setConfig({
			auth: config.token,
			baseUrl: config.base,
			credentials: config.credentials,
		});

		client.interceptors.request.use(async (request, _options) => {
			const token = await _auth.getLatestToken();
			request.headers.set('Authorization', `Bearer ${token}`);
			return request;
		});
	});
};
