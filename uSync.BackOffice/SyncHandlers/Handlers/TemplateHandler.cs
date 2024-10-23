using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler for Template items in Umbraco
/// </summary>
[SyncHandler(uSyncConstants.Handlers.TemplateHandler, "Templates", "Templates", uSyncConstants.Priorites.Templates,
    Icon = "icon-layout", EntityType = UdiEntityType.Template, IsTwoPass = true)]
public class TemplateHandler : SyncHandlerLevelBase<ITemplate>, ISyncHandler, ISyncPostImportHandler,
    INotificationAsyncHandler<SavedNotification<ITemplate>>,
    INotificationAsyncHandler<DeletedNotification<ITemplate>>,
    INotificationAsyncHandler<MovedNotification<ITemplate>>,
    INotificationAsyncHandler<SavingNotification<ITemplate>>,
    INotificationAsyncHandler<DeletingNotification<ITemplate>>,
    INotificationAsyncHandler<MovingNotification<ITemplate>>
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
    protected override async Task<IReadOnlyList<OrderedNodeInfo>> GetMergedItemsAsync(string[] folders)
    {
        var items = await base.GetMergedItemsAsync(folders);
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
                var templateContent = await GetTemplateContentAsync(item.Alias);
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

    private async Task<string> GetTemplateContentAsync(string alias)
    {
        if (_viewFileSystem is null) return string.Empty;

        var templateFileName = _viewFileSystem.GetRelativePath(alias.Replace(" ", "") + ".cshtml");
        if (templateFileName is null) return string.Empty;
        if (_viewFileSystem.FileExists(templateFileName) is false) return string.Empty;

        using (var stream = _viewFileSystem.OpenFile(templateFileName))
        {
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }

    /// <inheritdoc/>
    [Obsolete("Use the async version - removed in v16")]
    public IEnumerable<uSyncAction> ProcessPostImport(IEnumerable<uSyncAction> actions, HandlerSettings config)
        => ProcessPostImportAsync(actions, config).Result;
    public async Task<IEnumerable<uSyncAction>> ProcessPostImportAsync(IEnumerable<uSyncAction> actions, HandlerSettings config)
    {
        if (actions == null || !actions.Any()) return [];

        var results = new List<uSyncAction>();

        var options = new uSyncImportOptions {  Flags = SerializerFlags.LastPass };

        // we only do deletes here. 
        foreach (var action in actions.Where(x => x.Change == ChangeType.Hidden))
        {
            if (action.FileName is null) continue;
            results.AddRange(await ImportAsync(action.FileName, config, options));
        }

        return results;
    }


    /// <inheritdoc/>
    protected override string GetItemName(ITemplate item) => item.Name ?? item.Alias;

    protected override async Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key)
    {
        if (key == Guid.Empty) return await _templateService.GetChildrenAsync(-1);
        var template = await _templateService.GetAsync(key);
        if (template is null) return [];
        return await _templateService.GetChildrenAsync(template.Id);
    }


    protected override async Task<IEnumerable<IEntity>> GetFoldersAsync(Guid key)
        => await GetChildItemsAsync(key);

    /// <inheritdoc/>
    protected override string GetItemPath(ITemplate item, bool useGuid, bool isFlat)
        => useGuid ? item.Key.ToString() : item.Alias.ToSafeFileName(shortStringHelper);

    protected override async Task<IEnumerable<IEntity>> GetFoldersAsync(IEntity? parent)
    {
        if (parent is null) return [];
        return await _templateService.GetChildrenAsync(parent.Id);
    }
}
