
using Microsoft.Extensions.DependencyInjection;

using System.Linq;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

using uSync.Core.Cache;
using uSync.Core.DataTypes;
using uSync.Core.Dependency;
using uSync.Core.Mapping;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core
{
    public static class uSyncCoreBuilderExtensions
    {
        /// <summary>
        ///  Adds uSyncCore to project. 
        /// </summary>
        /// <remarks>
        ///  uSyncCore does not usally need to be registerd seperatly from 
        ///  uSync. unless you are using core features but not backoffice (files, handler) features
        /// </remarks>
        /// <param name="builder"></param>
        /// <returns></returns>

        public static IUmbracoBuilder AdduSyncCore(this IUmbracoBuilder builder)
        {
            // TODO: Check this - in theory, if SyncEnityCache is already registered we don't run again.
            if (builder.Services.FirstOrDefault(x => x.ServiceType == typeof(SyncEntityCache)) != null)
                return builder;

            // cache for entity items, we use it to speed up lookups.
            builder.Services.AddSingleton<SyncEntityCache>();

            // register *all* ConfigurationSerializers except those marked [HideFromTypeFinder]
            // has to happen before the DataTypeSerializer is loaded, because that is where
            // they are used
            builder.WithCollectionBuilder<ConfigurationSerializerCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<IConfigurationSerializer>());

            // value mappers, (map internal things in properties in and out of syncing process)
            builder.WithCollectionBuilder<SyncValueMapperCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<ISyncMapper>());

            // serializers - turn umbraco objects into / from xml in memeory. 
            builder.WithCollectionBuilder<SyncSerializerCollectionBuilder>()
                .Add(builder.TypeLoader.GetTypes<ISyncSerializerBase>());

            // the trackers, allow us to be more nuanced in tracking changes.
            builder.WithCollectionBuilder<SyncTrackerCollectionBuilder>()
                .Add(builder.TypeLoader.GetTypes<ISyncTrackerBase>());

            // Dependency checkers tell you what other umbraco objects an item needs to work
            builder.WithCollectionBuilder<SyncDependencyCollectionBuilder>()
                .Add(builder.TypeLoader.GetTypes<ISyncDependencyItem>());

            // the item factory lets us get to these collections from one place. 
            builder.Services.AddSingleton<ISyncItemFactory, SyncItemFactory>();

            return builder;
        }
    }
}
