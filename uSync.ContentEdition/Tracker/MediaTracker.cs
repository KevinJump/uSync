using Umbraco.Core.Models;

using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.ContentEdition.Tracker
{
    public class MediaTracker : ContentBaseTracker<IMedia>, ISyncTracker<IMedia>
    {
        public MediaTracker(ISyncSerializer<IMedia> serializer)
            : base(serializer)
        {
        }
    }
}
