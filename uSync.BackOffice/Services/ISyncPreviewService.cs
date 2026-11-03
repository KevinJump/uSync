using System;
using System.Threading.Tasks;
using System.Xml.Linq;

using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Interfaces;

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

    /// <summary>
    ///  get the current and incoming versions of an item from a uSync file that isn't 
    ///  (necessarily) in the uSync folders, e.g. one in a sync pack.
    /// </summary>
    /// <remarks>
    ///  <paramref name="findNode"/> finds the other items (by handler and key) the incoming item 
    ///  uses that aren't in Umbraco yet, so they come from the same place as the node. When it's 
    ///  null they come from the uSync folders.
    /// </remarks>
    /// <returns>null if the handler can't be found, or doesn't support previews</returns>
    Task<SyncPreviewResult?> GetPreviewAsync(string handlerAlias, XElement node, Func<ISyncHandler, Guid, Task<XElement?>>? findNode = null);
}
