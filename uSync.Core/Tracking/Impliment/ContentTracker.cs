
using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class ContentXmlTracker : ContentBaseTracker<IContent>, ISyncTracker<IContent>
{
    public ContentXmlTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}