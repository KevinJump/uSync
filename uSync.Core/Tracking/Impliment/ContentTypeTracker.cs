using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class ContentTypeTracker : ContentTypeBaseTracker<IContentType>, ISyncNodeTracker<IContentType>
    {
        public ContentTypeTracker(ISyncSerializer<IContentType> serializer) : base(serializer)
        {
        }
    }
}
