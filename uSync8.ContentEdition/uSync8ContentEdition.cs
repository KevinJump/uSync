
using Umbraco.Core.Components;
using Umbraco.Core.Models;
using Umbraco.Core.Composing;

using uSync8.ContentEdition.Serializers;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;
using uSync8.ContentEdition.Tracker;
using uSync8.Core;

namespace uSync8.ContentEdition
{
    [ComposeAfter(typeof(uSyncCoreComposer))]
    public class uSyncContentComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<ISyncSerializer<IContent>, ContentSerializer>();

            composition.Register<ISyncTracker<IContent>, ContentTracker>();
        }
    }
}
