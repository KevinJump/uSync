using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Components;
using uSync8.Core.Serialization;

namespace uSync8.Core
{
    public class USyncCoreComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // register *all* serializers, except those marked [HideFromTypeFinder]
            composition.WithCollectionBuilder<USyncSerializerCollectionBuilder>()
                .Add(() => composition.TypeLoader.GetTypes<ISyncSerializerBase>());
        }
    }
}
