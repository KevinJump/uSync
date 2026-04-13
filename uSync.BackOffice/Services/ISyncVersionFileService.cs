using System.Threading.Tasks;

namespace uSync.BackOffice.Services;

public interface ISyncVersionFileService
{
    Task<SyncFileVersionCheckResult> GetSyncFileInfo(string folder);
    Task WriteVersionFileAsync(string folder);
}