﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class MediaTracker : ContentBaseTracker<IMedia>, ISyncTracker<IMedia>
    {
        public MediaTracker(ISyncSerializer<IMedia> serializer) 
            : base(serializer)
        {
        }
    }
}
