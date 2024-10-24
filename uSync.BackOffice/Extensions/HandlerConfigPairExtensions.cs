using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Extensions;
internal static class HandlerConfigPairExtensions
{
    public static SyncHandlerView ToSyncHandlerView(this HandlerConfigPair pair, string handlerSet)
        => new SyncHandlerView
        {
            Alias = pair.Handler.Alias,
            Name = pair.Handler.Name,
            Group = pair.Handler.Group,
            Icon = pair.Handler.Icon,
            Enabled = pair.Handler.Enabled && pair.Settings.Enabled,
            Set = handlerSet
        };
}
