namespace uSync.BackOffice.Commands
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
