import { UmbEntryPointOnInit } from "@umbraco-cms/backoffice/extension-api";

import { manifests as localizations } from "./lang/manifest.js";
import { manifests as views } from "./views/manifest.js";
import { manifests as modals } from "./dialogs/manifest.js";
import { manifests as conditions } from "./condition/manifest.js";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "./api/client.gen.js";

export const onInit: UmbEntryPointOnInit = async (host, extensionRegistry) => {
  extensionRegistry.registerMany([
    ...localizations,
    ...views,
    ...modals,
    ...conditions,
  ]);

  const authContext = await host.getContext(UMB_AUTH_CONTEXT);
  if (!authContext) {
    console.warn(
      "UMB_AUTH_CONTEXT not available — extension API client will not be authenticated",
    );
    return;
  }
  authContext.configureClient(client);
};
