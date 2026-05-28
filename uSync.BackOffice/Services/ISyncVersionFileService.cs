using System.Threading.Tasks;

namespace uSync.BackOffice.Services;

/// <summary>
///  Controls the version file we write to disk on syncs (used to warn if sync is old)
/// </summary>
public interface ISyncVersionFileService
{
    /// <summary>
    ///  get the Sync file version information for a folder. 
    /// </summary>
    Task<SyncFileVersionCheckResult> GetSyncFileInfo(string folder);

    /// <summary>
    ///  write the version information to disk. 
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    Task WriteVersionFileAsync(string folder);
}