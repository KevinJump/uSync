using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Commands
{
    public enum SyncCommandResult
    {
        // errors - stop
        Error,

        // ok, continue
        Success = 100,
        NoResult = 499,

        // stop 
        Complete = 500,
        Restart
    }
}
