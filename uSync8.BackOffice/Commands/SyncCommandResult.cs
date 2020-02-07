using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Commands
{
    public enum SyncCommandResult
    {
        // ok, continue
        Success = 100,
        NoResult = 499,

        // stop 
        Complete = 500,
        Restart,

        // errors - stop
        Error = 1000

    }
}
