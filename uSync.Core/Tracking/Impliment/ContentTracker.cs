
using Umbraco.Cms.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class ContentXmlTracker : ContentBaseTracker<IContent>, ISyncNodeTracker<IContent>
    {
        public ContentXmlTracker(ISyncSerializer<IContent> serializer) : base(serializer)
        { }
    }

}