using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler for Template items in Umbraco
/// </summary>
[SyncHandler(uSyncConstants.Handlers.TemplateHandler, "Templates", "Templates", uSyncConstants.Priorites.Templates,
    Icon = "icon-layout", EntityType = UdiEntityType.Template, IsTwoPass = true)]
public class TemplateHandler : SyncHandlerLevelBase<ITemplate, IFileService>, ISyncHandler, ISyncPostImportHandler,
    INotificationHandler<SavedNotification<ITemplate>>,
    INotificationHandler<DeletedNotification<ITemplate>>,
    INotificationHandler<MovedNotification<ITemplate>>,
    INotificationHandler<SavingNotification<ITemplate>>,
    INotificationHandler<DeletingNotification<ITemplate>>,
    INotificationHandler<MovingNotification<ITemplate>>
{
    private readonly IFileSystem? _viewFileSystem;
    private readonly ITemplateService _templateService;

    private readonly ITemplateContentParserService _templateContentParserService;


    /// <inheritdoc/>
    public TemplateHandler(
        ILogger<TemplateHandler> logger,
        IEntityService entityService,
        ITemplateService templateService,   
        FileSystems fileSystems,
        ITemplateContentParserService templateContentParserService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    {
        _templateService = templateService;
        _viewFileSystem = fileSystems.MvcViewsFileSystem;
        _templateContentParserService = templateContentParserService;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<OrderedNodeInfo> GetMergedItems(string[] folders)
    {
        var items = base.GetMergedItems(folders);
        try
        {
            var results = new List<OrderedNodeInfo>();
            foreach (var item in items)
            {
                if (item.Level > 1000)
                {
                    results.Add(item);
                    continue;
                }

                // top level, lets check they aren't secretly lower down.
                var templateContent = GetTemplateContent(item.Alias);
                var masterAlias = _templateContentParserService.MasterTemplateAlias(templateContent);
                if (string.IsNullOrWhiteSpace(masterAlias) || masterAlias == item.Alias || masterAlias.InvariantEquals("null"))
                {
                    results.Add(item);
                    continue;
                }

                results.Add(new OrderedNodeInfo(item.FileName, item.Node, item.Level + 10, item.Path, item.IsRoot));
            }

            return [.. results.OrderBy(x => x.Level)];
        }
        catch(Exception ex)
        {
            logger.LogWarning(ex, "Error trying to sort the templates");
            return items; 
        }
    }

    private string GetTemplateContent(string alias)
    {
        if (_viewFileSystem is null) return string.Empty;

        var templateFileName = _viewFileSystem.GetRelativePath(alias.Replace(" ", "") + ".cshtml");
        if (templateFileName is null) return string.Empty;
        if (_viewFileSystem.FileExists(templateFileName) is false) return string.Empty;

        using (var stream = _viewFileSystem.OpenFile(templateFileName))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
    {
        if (actions == null || !actions.Any())
            return Enumerable.Empty<uSyncAction>();

        var results = new List<uSyncAction>();

        // we only do deletes here. 
        foreach (var action in actions.Where(x => x.Change == ChangeType.Hidden))
        {
            if (action.FileName is null) continue;

            results.AddRange(
                Import(action.FileName, config, SerializerFlags.LastPass));
        }

        return results;
    }
    
    /// <inheritdoc/>
    protected override string GetItemName(ITemplate item) => item.Name ?? item.Alias;

    /// <inheritdoc/>
    protected override IEnumerable<IEntity> GetChildItems(int parent)
        => _templateService.GetChildrenAsync(parent).Result;

    /// <inheritdoc/>
    protected override IEnumerable<IEntity> GetFolders(int parent)
        => GetChildItems(parent);

    /// <inheritdoc/>
    protected override string GetItemPath(ITemplate item, bool useGuid, bool isFlat)
        => useGuid ? item.Key.ToString() : item.Alias.ToSafeFileName(shortStringHelper);
}
