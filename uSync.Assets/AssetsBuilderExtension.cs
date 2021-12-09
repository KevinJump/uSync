using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.Assets
{
    public static class AssetsBuilderExtension
    {
        public static IUmbracoBuilder AdduSyncFiles(this IUmbracoBuilder builder)
        {

            // todo - we might need to confirm we haven't already added this 
            // (e.g if some uses the AdduSyncFiles() then the composer fires)
            // 
            // given this is just assets maybe we don't offer the Add Method??
            //

            // builder.ManifestFilters().Append<uSyncManifestFilter>();
            return builder;
        }
    }

    public class uSyncAssetsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<uSyncManifestFilter>();
        }
    }
}
