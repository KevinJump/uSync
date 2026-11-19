using System.Threading.Tasks;

namespace uSync.BackOffice.Startup;

/// <summary>
///  Saves and retrieves the status of the last import at startup.
/// </summary>
public interface ISyncStartupStatusService
{
    /// <summary>
    ///  get the stored startup import status (State is None when nothing is stored).
    /// </summary>
    Task<SyncStartupImportStatus> GetAsync();

    /// <summary>
    ///  store the startup import status.
    /// </summary>
    Task SetAsync(SyncStartupImportStatus status);
}
