using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core
{
    /// <summary>
    ///  indicates what happened to an item to cause a ghost file 
    ///  to be present. 
    /// </summary>
    public enum SyncActionType
    {
        None = 0,
        Rename = 1,
        Delete = 2
    }
}
