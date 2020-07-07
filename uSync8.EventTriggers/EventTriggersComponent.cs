using System.Linq;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services.Changes;
using Umbraco.Web.Cache;
using Umbraco.Web.PublishedCache;

using uSync8.BackOffice;

namespace uSync8.EventTriggers
{
    /// <summary>
    ///  Composer to register the events. 
    /// </summary>
    public class EventTriggersComposer : ComponentComposer<EventTriggersComponent> { }

    /// <summary>
    ///  Component, will trigger a cache rebuild when an import is completed. (and there are changes)
    /// </summary>
    public class EventTriggersComponent : IComponent
    {
        private readonly IPublishedSnapshotService snapshotService;

        public EventTriggersComponent(IPublishedSnapshotService snapshotService)
        {
            this.snapshotService = snapshotService;
        }

        public void Initialize()
        {
            if (UmbracoVersion.LocalVersion.Major == 8 && UmbracoVersion.LocalVersion.Minor < 4)
            {
                uSyncService.ImportComplete += BulkEventComplete;
            }
        }

        private void BulkEventComplete(uSyncBulkEventArgs e)
        {
            if (e.Actions.Any(x => x.Change > uSync8.Core.ChangeType.NoChange))
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

        private void RefreshContentCache(DistributedCache dc) {
            var payloads = new[] { new ContentCacheRefresher.JsonPayload(0, TreeChangeTypes.RefreshAll) };
            Umbraco.Web.Composing.Current.DistributedCache.RefreshByPayload(ContentCacheRefresher.UniqueId, payloads);
        }

        private void RefreshMediaCache(DistributedCache dc) {
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
            // end
        }
    }
}
