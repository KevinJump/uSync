
using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class ContentTypeTracker : ContentTypeBaseTracker<IContentType>, ISyncTracker<IContentType>
    {
        public ContentTypeTracker(SyncSerializerCollection serializers)
            : base(serializers)
        {
        }
    }
}
