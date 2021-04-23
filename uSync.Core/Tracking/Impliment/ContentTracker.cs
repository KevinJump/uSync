
using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class ContentXmlTracker : ContentBaseTracker<IContent>, ISyncTracker<IContent>
    {
        public ContentXmlTracker(ISyncSerializer<IContent> serializer) : base(serializer)
        { }
    }

}