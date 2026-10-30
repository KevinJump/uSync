using System;
using System.Threading.Tasks;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Services;

/// <summary>
///  builds previews of what an import would do to an item, without changing anything.
/// </summary>
public interface ISyncPreviewService
{
    /// <summary>
    ///  get the current and incoming versions of an item from the uSync folders, 
    ///  mapped by the first preview mapper that can map them.
    /// </summary>
    /// <remarks>
    ///  the mapper is called inside the preview scope, so it can read the database, 
    ///  and nothing it (or the dry run) writes is kept.
    /// </remarks>
    /// <returns>null if the handler or item can't be found, or the handler doesn't support previews</returns>
    Task<SyncPreviewResult?> GetPreviewAsync(string handlerAlias, Guid key);
}
