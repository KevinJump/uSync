using System.Collections.Generic;

using Umbraco.Cms.Core.Manifest;

namespace uSync.Assets
{
    public class uSyncManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            var manifest = new PackageManifest()
            {
                PackageName = "uSync",
                BundleOptions = BundleOptions.Independent,
                Scripts = new string[]
                {
                    "/_content/uSync.Assets/usync.9.2.0.min.js"
                },
                Stylesheets = new string[]
                {
                    "/_content/uSync.Assets/usync.9.2.0.min.css"
                }
            };

            manifests.Add(manifest);            
        }
    }
}
