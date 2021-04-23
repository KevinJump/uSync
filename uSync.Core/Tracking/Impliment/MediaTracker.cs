using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class MediaTracker : ContentBaseTracker<IMedia>, ISyncTracker<IMedia>
    {
        public MediaTracker(ISyncSerializer<IMedia> serializer)
            : base(serializer)
        {
        }
    }
}
