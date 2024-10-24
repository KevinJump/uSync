using System;

using uSync.BackOffice.Services;

namespace uSync.BackOffice;

/// <summary>
///  wraps code running that might trigger events, pausing uSyncs capture of those events.
/// </summary>
public class uSyncImportPause : IDisposable
{
    private readonly ISyncEventService _mutexService;

    /// <summary>
    /// Generate a pause object
    /// </summary>
    public uSyncImportPause(ISyncEventService mutexService)
    {
        _mutexService = mutexService;
        _mutexService.Pause();
    }

    /// <summary>
    ///  generate a pause object, but only pause if told to do so.
    /// </summary>
    public uSyncImportPause(ISyncEventService mutexService, bool pause)
    {
        _mutexService = mutexService;
        if (pause)
            _mutexService.Pause();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        _mutexService.UnPause();
    }
}
