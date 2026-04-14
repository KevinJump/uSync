import { UmbEntryPointOnInit } from "@umbraco-cms/backoffice/extension-api";

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
};
