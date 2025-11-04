import { uSyncConstants } from "@jumoo/uSync";

const workspace: UmbExtensionManifest = {
  type: "workspaceView",
  alias: "historyView",
  name: "uSync history",
  js: () => import("./history-view.element.js"),
  weight: 125,
  meta: {
    label: "History",
    pathname: "history",
    icon: "icon-history",
  },
  conditions: [
    {
      alias: "Umb.Condition.WorkspaceAlias",
      match: uSyncConstants.workspace.alias,
    },
  ],
};

export const manifests = [workspace];
