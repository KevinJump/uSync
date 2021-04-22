using Umbraco.Cms.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class MediaTracker : ContentBaseTracker<IMedia>, ISyncNodeTracker<IMedia>
    {
        public MediaTracker(ISyncSerializer<IMedia> serializer)
            : base(serializer)
        {
        }
    }
}
