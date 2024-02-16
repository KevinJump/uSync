
using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class MediaTypeTracker : ContentTypeBaseTracker<IMediaType>, ISyncTracker<IMediaType>
{
    public MediaTypeTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}
