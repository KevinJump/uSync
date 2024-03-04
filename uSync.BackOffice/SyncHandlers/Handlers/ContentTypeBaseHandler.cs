using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  handler base for all ContentTypeBase handlers
/// </summary>
public abstract class ContentTypeBaseHandler<TObject, TService> : SyncHandlerContainerBase<TObject, TService>
    where TObject : ITreeEntity
    where TService : IService

{

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ContentTypeBaseHandler(
        ILogger<SyncHandlerContainerBase<TObject, TService>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    { }

    /// <inheritdoc />
    protected override SyncAttempt<XElement> Export_DoExport(TObject item, string filename, string[] folders, HandlerSettings config)
    {
        // all the possible files that there could be. 
        var files = folders.Select(x => GetPath(x, item, config.GuidNames, config.UseFlatStructure)).ToArray();
        var nodes = syncFileService.GetAllNodes(files[..^1]);

        // with roots enabled - we attempt to merge doctypes ! 
        // 
        var attempt = SerializeItem(item, new Core.Serialization.SyncSerializerOptions(config.Settings));
        if (attempt.Success && attempt.Item is not null)
        {
            if (ShouldExport(attempt.Item, config))
            {
                if (nodes.Count > 0)
                {
                    nodes.Add(attempt.Item);
                    var difference = syncFileService.GetDifferences(nodes, trackers.FirstOrDefault());
                    if (difference != null)
                    {
                        syncFileService.SaveXElement(difference, filename);
                    }
                    else
                    {
                        if (syncFileService.FileExists(filename))
                            syncFileService.DeleteFile(filename);
                    }

                }
                else
                {
                    syncFileService.SaveXElement(attempt.Item, filename);
                }

                if (config.CreateClean && HasChildren(item))
                    CreateCleanFile(GetItemKey(item), filename);
            }
            else
            {
                return SyncAttempt<XElement>.Succeed(filename, ChangeType.NoChange, "Not Exported (Based on configuration)");
            }
        }

        return attempt;
    }
}
