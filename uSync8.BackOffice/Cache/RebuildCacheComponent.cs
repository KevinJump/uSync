using System.Linq;

using Umbraco.Core.Composing;
using Umbraco.Core.Services.Changes;
using Umbraco.Web.Cache;
using Umbraco.Web.PublishedCache;
using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.Cache
{
    [ComposeBefore(typeof(uSyncBackOfficeComposer))]
    public class RebuildCacheComposer : ComponentComposer<RebuildCacheComponent> { }

    
    public class RebuildCacheComponent : IComponent
    {
        private readonly IPublishedSnapshotService snapshotService;
        private uSyncSettings settings;

        public RebuildCacheComponent(IPublishedSnapshotService snapshotService)
        {
            this.snapshotService = snapshotService;

            this.settings = Current.Configs.uSync();

            uSyncConfig.Reloaded += Config_Reloaded;
        }

        private void Config_Reloaded(uSyncSettings settings)
        {
            this.settings = Current.Configs.uSync();
        }

        public void Initialize()
        {
            uSyncService.ImportComplete += ImportComplete;
           
        }

        private void ImportComplete(uSyncBulkEventArgs e)
        {
            if (settings.RebuildCacheOnCompleation &&
                e.Actions.Any(x => x.Change > uSync8.Core.ChangeType.NoChange))
            {
                // change happened. - rebuild
                snapshotService.Rebuild();

                // then refresh the cache : 
                // there is a function for this but it is internal, so we have extracted bits.
                // mimics => DistributedCache.RefreshAllPublishedSnapshot
                RefreshContentCache(Umbraco.Web.Composing.Current.DistributedCache);
                RefreshMediaCache(Umbraco.Web.Composing.Current.DistributedCache);
                RefreshAllDomainCache(Umbraco.Web.Composing.Current.DistributedCache);
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
