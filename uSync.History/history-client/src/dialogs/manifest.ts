const modal: UmbExtensionManifest = {
  type: "modal",
  alias: "usync.history.modal",
  name: "uSync History",
  js: () => import("./history-modal.element.js"),
};

export const manifests = [modal];
