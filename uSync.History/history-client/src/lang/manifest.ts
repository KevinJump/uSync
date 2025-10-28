const localization: UmbExtensionManifest = {
  type: "localization",
  alias: "usync.history.localisation",
  name: "uSync History",
  js: () => import("./en.js"),
  meta: {
    culture: "en",
  },
};

export const manifests = [localization];
