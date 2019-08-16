using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    public class ExtendedHandlerConfigPair
    {
        public ISyncExtendedHandler Handler { get; set; }
        public HandlerSettings Settings { get; set; }

    }
}
