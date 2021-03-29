﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core.Serialization
{
    public interface ISyncCachedSerializer
    {
        void InitializeCache();

        void DisposeCache();
    }
}
