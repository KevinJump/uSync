using Umbraco.Cms.Core.Manifest;

namespace uSync.History
{
    internal class uSyncHistoryManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            manifests.Add(new PackageManifest
            {
                PackageId = "uSyncHistory",
                PackageName = "uSync History",
                Version = "1.0",
                AllowPackageTelemetry = true,
                BundleOptions = BundleOptions.Independent,
                Scripts = new[]
                {
                    "/App_Plugins/uSyncHistory/dashboard.controller.js"
                }
            });
        }
    }
}
