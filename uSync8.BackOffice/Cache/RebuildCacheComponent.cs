using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Services.Changes;
using Umbraco.Web.Cache;
using Umbraco.Web.PublishedCache;

using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.Cache
{
    [ComposeBefore(typeof(uSyncBackOfficeComposer))]
    public class RebuildCacheComposer : ComponentComposer<RebuildCacheComponent> { }

    /// <summary>
    ///  Cache rebuilding, when imports are completed
    /// </summary>
    /// <remarks>
    ///  This is off by default, the RebuildCacheOnCompletion setting is false in the 
    ///  default config, and this probibly isn't needed past Umbraco 8.3.
    /// </remarks>
    public class RebuildCacheComponent : IComponent
    {
        private readonly IPublishedSnapshotService snapshotService;
        private bool rebuildCacheOnCompleaton;

        public RebuildCacheComponent(IPublishedSnapshotService snapshotService)
        {
            this.snapshotService = snapshotService;
            uSyncConfig.Reloaded += Config_Reloaded;
        }

        private void Config_Reloaded(uSyncSettings settings)
        {
            rebuildCacheOnCompleaton = settings.RebuildCacheOnCompletion;
        }

        public void Initialize()
        {
            // this only is ever required on pre v8.4 
            if (UmbracoVersion.LocalVersion.Major != 8 || UmbracoVersion.LocalVersion.Minor < 4)
            {
                var config = Current.Configs.uSync();
                if (config != null)
                    Config_Reloaded(config);

                uSyncService.ImportComplete += ImportComplete;
            }
        }

        private void ImportComplete(uSyncBulkEventArgs e)
        {
            if (rebuildCacheOnCompleaton &&
                e.Actions.Any(x => x.Change > uSync8.Core.ChangeType.NoChange))
            {
                // change happened. - rebuild
                snapshotService.Rebuild();

                if (UmbracoVersion.LocalVersion.Major == 8 && UmbracoVersion.LocalVersion.Minor < 4)
                {
                    // we only do this on v8.3 and below. 

                    // then refresh the cache : 
                    // there is a function for this but it is internal, so we have extracted bits.
                    // mimics => DistributedCache.RefreshAllPublishedSnapshot
                    RefreshContentCache(Umbraco.Web.Composing.Current.DistributedCache);
                    RefreshMediaCache(Umbraco.Web.Composing.Current.DistributedCache);
                    RefreshAllDomainCache(Umbraco.Web.Composing.Current.DistributedCache);
                }
            }
        }


        private void RefreshContentCache(DistributedCache dc)
        {
            var payloads = new[] { new ContentCacheRefresher.JsonPayload(0, TreeChangeTypes.RefreshAll) };
            dc.RefreshByPayload(ContentCacheRefresher.UniqueId, payloads);
        }

        private void RefreshMediaCache(DistributedCache dc)
        {
            var payloads = new[] { new MediaCacheRefresher.JsonPayload(0, TreeChangeTypes.RefreshAll) };
            dc.RefreshByPayload(MediaCacheRefresher.UniqueId, payloads);
        }

        public void RefreshAllDomainCache(DistributedCache dc)
        {
            var payloads = new[] { new DomainCacheRefresher.JsonPayload(0, DomainChangeTypes.RefreshAll) };
            dc.RefreshByPayload(DomainCacheRefresher.UniqueId, payloads);
        }


        public void Terminate()
        {
            // end. 
        }
    }
}
