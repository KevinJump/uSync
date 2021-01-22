using System.Linq;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class MemberTypeTracker : ContentTypeBaseTracker<IMemberType>, ISyncNodeTracker<IMemberType>
    {
        public MemberTypeTracker(ISyncSerializer<IMemberType> serializer) : base(serializer)
        { }
    }
}
