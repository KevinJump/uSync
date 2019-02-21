
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Composing;

using uSync8.ContentEdition.Serializers;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;
using uSync8.ContentEdition.Tracker;
using uSync8.Core;
using uSync8.BackOffice;
using uSync8.BackOffice.Models;

namespace uSync8.ContentEdition
{
    public class uSyncContent : ISyncAddOn
    {
        public string Name => "Content Edition";
        public string Version => "8.0.0";
    }

    [ComposeAfter(typeof(uSyncCoreComposer))]
    [ComposeBefore(typeof(uSyncBackOfficeComposer))]
    public class uSyncContentComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<ISyncSerializer<IContent>, ContentSerializer>();
            composition.Register<ContentTemplateSerializer>();
            composition.Register<ISyncSerializer<IMedia>, MediaSerializer>();
            composition.Register<ISyncSerializer<IDictionaryItem>, DictionaryItemSerializer>();
            composition.Register<ISyncSerializer<IDomain>, DomainSerializer>();

            composition.Register<ISyncTracker<IContent>, ContentTracker>();
            composition.Register<ISyncTracker<IMedia>, MediaTracker>();
            composition.Register<ISyncTracker<IDictionaryItem>, DictionaryItemTracker>();
            composition.Register<ISyncTracker<IDomain>, DomainTracker>();

        }
    }
}
