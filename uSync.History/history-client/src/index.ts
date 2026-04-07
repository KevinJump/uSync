import { UmbEntryPointOnInit } from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "./api/client.gen.js";

import { manifests as localizations } from "./lang/manifest.js";
import { manifests as views } from "./views/manifest.js";
import { manifests as modals } from "./dialogs/manifest.js";
import { manifests as conditions } from "./condition/manifest.js";

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
  extensionRegistry.registerMany([
    ...localizations,
    ...views,
    ...modals,
    ...conditions,
  ]);

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
      request.headers.set("Authorization", `Bearer ${token}`);
      return request;
    });
  });
};
