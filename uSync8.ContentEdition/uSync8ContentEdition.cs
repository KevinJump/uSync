
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
using uSync8.ContentEdition.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync8.ContentEdition
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncContent : ISyncAddOn
    {
        public string Name => "Content Edition";
        public string Version => "8.0.1";

        /// The following if you are an add on that displays like an app
        
        // but content edition doesn't have an interface, so the view is empty. this hides it. 
        public string View => string.Empty;
        public string Icon => "icon-globe";
        public string Alias => "Content";
        public string DisplayName => "Content";

        public int SortOrder => 10;

        public static int DependencyCountMax = 204800;
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
            composition.Register<ISyncSerializer<IRelationType>, RelationTypeSerializer>();

            composition.Register<ISyncTracker<IContent>, ContentTracker>();
            composition.Register<ISyncTracker<IMedia>, MediaTracker>();
            composition.Register<ISyncTracker<IDictionaryItem>, DictionaryItemTracker>();
            composition.Register<ISyncTracker<IDomain>, DomainTracker>();
            composition.Register<ISyncTracker<IRelationType>, RelationTypeTracker>();

            composition.WithCollectionBuilder<SyncValueMapperCollectionBuilder>()
                .Add(composition.TypeLoader.GetTypes<ISyncMapper>());
        }
    }
}
