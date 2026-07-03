using System.Collections.Generic;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Models.Entities;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers.Interfaces;

/// <summary>
///  handlers whose items live inside container (folder) items that have to be
///  exported as items in their own right.
/// </summary>
/// <remarks>
///  Most handlers capture an item's folder location inside the item's own serialized
///  file, so the folders travel with the item. Some handlers (for example the Library
///  Element handler) instead keep the folders as separate container items which have to
///  be exported individually.
///
///  During a full export (<c>ExportAllAsync</c>) these handlers already export their
///  containers, but callers that work item-by-item - such as a dependency based push -
///  never hit that path. Implementing this interface lets those callers export a
///  container when they come across one, so the folder structure is included.
///
///  The import side already detects and handles container nodes, so only the export
///  needs exposing here.
/// </remarks>
public interface ISyncContainerHandler
{
    /// <summary>
    ///  Export a single container (folder) item, identified by its entity, to disk.
    /// </summary>
    /// <param name="item">the container entity to export</param>
    /// <param name="folders">the handler folders to export into</param>
    /// <param name="config">handler settings to use for the export</param>
    /// <returns>the actions describing what was exported</returns>
    Task<IEnumerable<uSyncAction>> ExportContainer(IEntity item, string[] folders, HandlerSettings config);
}
