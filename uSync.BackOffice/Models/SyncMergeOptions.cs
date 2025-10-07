using uSync.BackOffice.SyncHandlers.Interfaces;

namespace uSync.BackOffice.Models;

public class SyncMergeOptions
{
    public SyncMergeOptions() { }

    public SyncMergeOptions(SyncUpdateCallback? callback)
    {
        UpdateCallback = callback;
    }

    public SyncUpdateCallback? UpdateCallback;
}