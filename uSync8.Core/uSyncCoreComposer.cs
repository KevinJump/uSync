using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;

using uSync8.Core.DataTypes;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Serialization.Serializers;
using uSync8.Core.Tracking;

namespace uSync8.Core
{
    public class uSyncCoreComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            /*
            // register *all* serializers, except those marked [HideFromTypeFinder]
            composition.WithCollectionBuilder<USyncSerializerCollectionBuilder>()
                .Add(() => composition.TypeLoader.GetTypes<ISyncSerializerBase>());
                */

            // register *all* ConfigurationSerializers except those marked [HideFromTypeFinder]
            // has to happen before the DataTypeSerializer is loaded, because that is where
            // they are used
            composition.WithCollectionBuilder<ConfigurationSerializerCollectionBuilder>()
                .Add(() => composition.TypeLoader.GetTypes<IConfigurationSerializer>());

            // register the core handlers (we will refactor to make this dynamic)
            composition.Register<ISyncSerializer<IContentType>, ContentTypeSerializer>();
            composition.Register<ISyncSerializer<IMediaType>, MediaTypeSerializer>();
            composition.Register<ISyncSerializer<IMemberType>, MemberTypeSerializer>();
            composition.Register<ISyncSerializer<ITemplate>, TemplateSerializer>();
            composition.Register<ISyncSerializer<ILanguage>, LanguageSerializer>();
            composition.Register<ISyncSerializer<IMacro>, MacroSerializer>();
            composition.Register<ISyncSerializer<IDataType>, DataTypeSerializer>();


            // the trackers, allow us to be more nuanced in tracking changes, should
            // mean change messages are better. 
            composition.WithCollectionBuilder<SyncTrackerCollectionBuilder>()
                .Add(composition.TypeLoader.GetTypes<ISyncTrackerBase>());

            // load the dependency checkers from a collection
            // allows us to extend the dependency checks without changing the core. 
            composition.WithCollectionBuilder<SyncDependencyCollectionBuilder>()
                .Add(composition.TypeLoader.GetTypes<ISyncDependencyItem>());
        }
    }
}
