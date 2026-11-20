namespace uSync.BackOffice.Startup;

/// <summary>
///  the state of the last import at startup.
/// </summary>
public enum SyncStartupImportState
{
    /// <summary>
    ///  nothing has been recorded.
    /// </summary>
    None,

    /// <summary>
    ///  import at startup is not switched on.
    /// </summary>
    NotConfigured,

    /// <summary>
    ///  the import was blocked by the stop file.
    /// </summary>
    Blocked,

    /// <summary>
    ///  the import is running.
    /// </summary>
    Running,

    /// <summary>
    ///  the import completed without errors.
    /// </summary>
    Completed,

    /// <summary>
    ///  the import completed, but some items had errors.
    /// </summary>
    CompletedWithErrors,

    /// <summary>
    ///  the import failed.
    /// </summary>
    Failed
}
