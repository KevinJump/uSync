using Umbraco.Core.Models;

using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class MediaTracker : ContentBaseTracker<IMedia>, ISyncNodeTracker<IMedia>
    {
        public MediaTracker(ISyncSerializer<IMedia> serializer)
            : base(serializer)
        {
        }
    }
}
