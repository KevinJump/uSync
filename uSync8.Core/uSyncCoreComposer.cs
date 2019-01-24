using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Components;
using Umbraco.Core.Models;
using Umbraco.Core.Composing;
using uSync8.Core.Serialization;
using uSync8.Core.Serialization.Serializers;

namespace uSync8.Core
{
    public class USyncCoreComposer : IUserComposer
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
        }
    }
}
