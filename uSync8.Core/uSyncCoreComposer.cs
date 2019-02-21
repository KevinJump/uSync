using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Composing;
using uSync8.Core.Serialization;
using uSync8.Core.Serialization.Serializers;
using uSync8.Core.Tracking;
using uSync8.Core.Tracking.Impliment;

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
            composition.Register<ISyncTracker<IContentType>, ContentTypeTracker>();
            composition.Register<ISyncTracker<IMediaType>, MediaTypeTracker>();
            composition.Register<ISyncTracker<IMemberType>, MemberTypeTracker>();
            composition.Register<ISyncTracker<ITemplate>, TemplateTracker>();
            composition.Register<ISyncTracker<ILanguage>, LanguageTracker>();
            composition.Register<ISyncTracker<IMacro>, MacroTracker>();
            composition.Register<ISyncTracker<IDataType>, DataTypeTracker>();
        }
    }
}
