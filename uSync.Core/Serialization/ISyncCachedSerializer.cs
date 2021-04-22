using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync.Core.Serialization
{
    public interface ISyncCachedSerializer
    {
        void InitializeCache();

        void DisposeCache();
    }
}
