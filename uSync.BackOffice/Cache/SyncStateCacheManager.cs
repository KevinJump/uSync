using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Events;

using uSync.Core;

namespace uSync.BackOffice.Cache;

/// <summary>
///  plugs the state cache into uSync's own import, report and export notifications.
/// </summary>
/// <remarks>
///  <para>
///   deliberately done through notifications rather than by changing the import pipeline. uSync
///   already fires a cancelable notification per item on both the import and report paths - the
///   report one exists for exactly this reason ("this lets us intercept a report and shortcut
///   the checking") - so the whole feature can hang off the side without touching how items are
///   deserialized or compared.
///  </para>
///  <para>
///   the three things we do:
///   <list type="bullet">
///    <item>before an item is checked, cancel it if we already know the file matches</item>
///    <item>after an item is checked, remember it if the answer was "no change"</item>
///    <item>after an item is exported, remember it - we just wrote the file from the database,
///          so the two sides match by construction</item>
///   </list>
///  </para>
/// </remarks>
internal class SyncStateCacheManager :
    INotificationAsyncHandler<uSyncImportStartingNotification>,
    INotificationAsyncHandler<uSyncReportStartingNotification>,
    INotificationAsyncHandler<uSyncExportStartingNotification>,
    INotificationAsyncHandler<uSyncImportCompletedNotification>,
    INotificationAsyncHandler<uSyncReportCompletedNotification>,
    INotificationAsyncHandler<uSyncExportCompletedNotification>,
    INotificationAsyncHandler<uSyncImportingItemNotification>,
    INotificationAsyncHandler<uSyncReportingItemNotification>,
    INotificationAsyncHandler<uSyncImportedItemNotification>,
    INotificationAsyncHandler<uSyncReportedItemNotification>,
    INotificationAsyncHandler<uSyncExportedItemNotification>
{
    private readonly ISyncStateCache _stateCache;
    private readonly ILogger<SyncStateCacheManager> _logger;

    private int _checked;
    private int _skipped;

    public SyncStateCacheManager(
        ISyncStateCache stateCache,
        ILogger<SyncStateCacheManager> logger)
    {
        _stateCache = stateCache;
        _logger = logger;
    }

    #region Run start and end

    /// <inheritdoc/>
    public Task HandleAsync(uSyncImportStartingNotification notification, CancellationToken c) => StartRunAsync();

    /// <inheritdoc/>
    public Task HandleAsync(uSyncReportStartingNotification notification, CancellationToken c) => StartRunAsync();

    /// <inheritdoc/>
    public Task HandleAsync(uSyncExportStartingNotification notification, CancellationToken c) => StartRunAsync();

    /// <inheritdoc/>
    public Task HandleAsync(uSyncImportCompletedNotification notification, CancellationToken c) => EndRunAsync("Import");

    /// <inheritdoc/>
    public Task HandleAsync(uSyncReportCompletedNotification notification, CancellationToken c) => EndRunAsync("Report");

    /// <inheritdoc/>
    public Task HandleAsync(uSyncExportCompletedNotification notification, CancellationToken c) => EndRunAsync("Export");

    private async Task StartRunAsync()
    {
        Interlocked.Exchange(ref _checked, 0);
        Interlocked.Exchange(ref _skipped, 0);

        if (_stateCache.IsEnabled is false) return;
        await _stateCache.LoadAsync();
    }

    private async Task EndRunAsync(string operation)
    {
        // persist runs even when the setting is off - a run can fold invalidations out of the
        // cache file, and those are the one thing we must not lose.
        await _stateCache.PersistAsync();

        var skipped = Volatile.Read(ref _skipped);
        if (skipped > 0 && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "uSync {operation}: state cache skipped {skipped} of {total} items (no database lookup or serialize needed)",
                operation, skipped, Volatile.Read(ref _checked));
        }
    }

    #endregion

    #region Skipping items we already know about

    /// <inheritdoc/>
    public async Task HandleAsync(uSyncImportingItemNotification notification, CancellationToken c)
    {
        if (notification.Force) return;
        if (await IsKnownCurrentAsync(notification.Item) is false) return;

        notification.Cancel = true;
        notification.Message = "No change (from the state cache)";
    }

    /// <inheritdoc/>
    public async Task HandleAsync(uSyncReportingItemNotification notification, CancellationToken c)
    {
        if (notification.Force) return;
        if (await IsKnownCurrentAsync(notification.Item) is false) return;

        notification.Cancel = true;
        notification.Message = "No change (from the state cache)";
    }

    private async Task<bool> IsKnownCurrentAsync(XElement? node)
    {
        if (_stateCache.IsEnabled is false || node is null) return false;

        Interlocked.Increment(ref _checked);

        if (await _stateCache.IsKnownCurrentAsync(node) is false) return false;

        Interlocked.Increment(ref _skipped);
        return true;
    }

    #endregion

    #region Learning which items match

    /// <inheritdoc/>
    public Task HandleAsync(uSyncImportedItemNotification notification, CancellationToken c)
        => RecordIfUnchangedAsync(notification);

    /// <inheritdoc/>
    public Task HandleAsync(uSyncReportedItemNotification notification, CancellationToken c)
        => RecordIfUnchangedAsync(notification);

    /// <summary>
    ///  remember the file only when the full check ran and told us it matches.
    /// </summary>
    /// <remarks>
    ///  specifically *not* recording after a successful update or create. it is tempting to
    ///  assume the two sides now agree, but they do not always: some items don't round-trip
    ///  exactly, which is what uSync's "xml is different - but properties may not have changed"
    ///  message is telling you. recording those would silence a real difference for good.
    ///  instead they get checked again next time, which is both honest and self-correcting.
    /// </remarks>
    private async Task RecordIfUnchangedAsync(uSyncItemNotification<XElement> notification)
    {
        if (_stateCache.IsEnabled is false) return;
        if (notification.Change != ChangeType.NoChange) return;
        if (notification.Item is null) return;

        await _stateCache.RecordAsync(notification.Item);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///  an export has just written this xml to disk straight from the database, so the file and
    ///  the database agree by construction. this is what makes an export a way to warm the cache
    ///  up without a slow first import.
    /// </remarks>
    public async Task HandleAsync(uSyncExportedItemNotification notification, CancellationToken c)
    {
        if (_stateCache.IsEnabled is false || notification.Item is null) return;
        await _stateCache.RecordAsync(notification.Item);
    }

    #endregion
}
