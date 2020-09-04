using System;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers
{
    [Obsolete("Extended Handler Config gives better results")]
    public class HandlerConfigPair
    {
        public ISyncHandler Handler { get; set; }
        public HandlerSettings Settings { get; set; }
    }
}
