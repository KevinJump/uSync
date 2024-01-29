
using System.Diagnostics;
using System.Reflection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice;
using uSync.BackOffice.Assets.Notifications;

namespace uSync.Backoffice.Assets
{
    public class StaticAssetsBoot : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AdduSyncStaticAssets();
        }
    }

    public static class uSyncStaticAssetsExtensions
    {
        public static IUmbracoBuilder AdduSyncStaticAssets(this IUmbracoBuilder builder)
        {
            // don't add if the filter is already there .
            if (builder.ManifestFilters().Has<uSyncAssetManifestFilter>())
                return builder;

            // add the package manifest programmatically. 
            builder.ManifestFilters().Append<uSyncAssetManifestFilter>();

            // add the javascript variables 
            builder.AddNotificationHandler<ServerVariablesParsingNotification, uSyncServerVariablesHandler>();


            return builder;
        }
    }

    internal class uSyncAssetManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            var assembly = typeof(uSyncAssetManifestFilter).Assembly;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductVersion;
            
            manifests.Add(new PackageManifest
            {
                PackageId = "uSync",
                PackageName = uSyncConstants.Package.Name,
                Version = assembly.GetName().Version.ToString(3),
                AllowPackageTelemetry = true,
                BundleOptions = BundleOptions.None,
                Scripts = new[]
                {
                    $"{uSyncConstants.Package.PluginPath}/usync.{version}.min.js"
                },
                Stylesheets = new[]
                {
                    $"{uSyncConstants.Package.PluginPath}/usync.{version}.min.css"
                }
            }); ;
        }
    }
}
