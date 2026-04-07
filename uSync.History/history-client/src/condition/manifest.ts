export const manifests: UmbExtensionManifest[] = [
  {
    type: "condition",
    alias: "Umb.Condition.SyncHistoryEnabled",
    name: "Sync History Enabled Condition",
    js: () => import("./history-enabled.condition.ts"),
  },
];
