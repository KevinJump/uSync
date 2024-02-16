﻿
using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class MemberTypeTracker : ContentTypeBaseTracker<IMemberType>, ISyncTracker<IMemberType>
{
    public MemberTypeTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}
