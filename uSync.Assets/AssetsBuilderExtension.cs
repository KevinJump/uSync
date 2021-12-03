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
            builder.ManifestFilters().Append<uSyncManifestFilter>();
            return builder;
        }
    }

    public class uSyncAssetsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AdduSyncFiles();
        }
    }
}
