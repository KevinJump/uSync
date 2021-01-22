using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class MediaTypeTracker : ContentTypeBaseTracker<IMediaType>, ISyncNodeTracker<IMediaType>
    {
        public MediaTypeTracker(ISyncSerializer<IMediaType> serializer) : base(serializer)
        { }
    }
}
