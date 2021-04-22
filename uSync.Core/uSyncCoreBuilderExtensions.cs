
using System.Linq;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

using uSync.Core.Cache;
using uSync.Core.DataTypes;
using uSync.Core.Dependency;
using uSync.Core.Mapping;
using uSync.Core.Serialization;
using uSync.Core.Serialization.Serializers;
using uSync.Core.Tracking;

namespace uSync.Core
{
    public static class uSyncCoreBuilderExtensions
    {
        public static IUmbracoBuilder AdduSyncCore(this IUmbracoBuilder builder)
        {
            // TODO: Check this - in theory, if SyncEnityCache is already registered we don't run again.
            if (builder.Services.FirstOrDefault(x => x.ServiceType == typeof(SyncEntityCache)) != null)
                return builder;

            // cache for entity items, we use it to speed up lookups.
            builder.Services.AddUnique<SyncEntityCache>();

            // register *all* ConfigurationSerializers except those marked [HideFromTypeFinder]
            // has to happen before the DataTypeSerializer is loaded, because that is where
            // they are used
            builder.WithCollectionBuilder<ConfigurationSerializerCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<IConfigurationSerializer>());

            builder.WithCollectionBuilder<SyncValueMapperCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<ISyncMapper>());

            // register the core handlers (we will refactor to make this dynamic)
            builder.Services.AddUnique<ISyncSerializer<IContentType>, ContentTypeSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IMediaType>, MediaTypeSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IMemberType>, MemberTypeSerializer>();
            builder.Services.AddUnique<ISyncSerializer<ITemplate>, TemplateSerializer>();
            builder.Services.AddUnique<ISyncSerializer<ILanguage>, LanguageSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IMacro>, MacroSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IDataType>, DataTypeSerializer>();

            // content edition ones. 
            builder.Services.AddUnique<ISyncSerializer<IContent>, ContentSerializer>();
            builder.Services.AddUnique<ContentTemplateSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IMedia>, MediaSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IDictionaryItem>, DictionaryItemSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IDomain>, DomainSerializer>();
            builder.Services.AddUnique<ISyncSerializer<IRelationType>, RelationTypeSerializer>();

            // the trackers, allow us to be more nuanced in tracking changes, should
            // mean change messages are better. 
            builder.WithCollectionBuilder<SyncTrackerCollectionBuilder>()
                .Add(builder.TypeLoader.GetTypes<ISyncTrackerBase>());

            // load the dependency checkers from a collection
            // allows us to extend checkers without changing the core. 
            builder.WithCollectionBuilder<SyncDependencyCollectionBuilder>()
                .Add(builder.TypeLoader.GetTypes<ISyncDependencyItem>());

            // item factory, makes all the constructors of handlers way simpler
            builder.Services.AddUnique<ISyncItemFactory, SyncItemFactory>();
            
            return builder;
        }
    }
}
