using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class ContentXmlTracker : ContentBaseTracker<IContent>, ISyncNodeTracker<IContent>
    {
        public ContentXmlTracker(ISyncSerializer<IContent> serializer) : base(serializer)
        { }
    }

}