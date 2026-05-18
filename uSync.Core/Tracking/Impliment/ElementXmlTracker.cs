
using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class ElementXmlTracker : ContentBaseTracker<IElement>, ISyncTracker<IElement>
{
    public ElementXmlTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}