using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class MediaTypeTracker : ContentTypeBaseTracker<IMediaType>, ISyncTracker<IMediaType>
    {
        public MediaTypeTracker(ISyncSerializer<IMediaType> serializer) : base(serializer)
        { }
    }
}
