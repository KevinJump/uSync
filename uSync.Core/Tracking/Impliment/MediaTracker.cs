using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class MediaTracker : ContentBaseTracker<IMedia>, ISyncTracker<IMedia>
{
    public MediaTracker(SyncSerializerCollection serializers)
        : base(serializers)
    {
    }
}
