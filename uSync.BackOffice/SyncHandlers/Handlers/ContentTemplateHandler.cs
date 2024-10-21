using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to manage content templates in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.ContentTemplateHandler, "Blueprints", "Blueprints", uSyncConstants.Priorites.ContentTemplate
    , Icon = "icon-blueprint", IsTwoPass = true, EntityType = UdiEntityType.DocumentBlueprint)]
public class ContentTemplateHandler : ContentHandlerBase<IContent>, ISyncHandler,
    INotificationAsyncHandler<ContentSavedBlueprintNotification>,
    INotificationAsyncHandler<ContentDeletedBlueprintNotification>
{
    /// <summary>
    /// ContentTypeHandler belongs to the Content group by default
    /// </summary>
    public override string Group => uSyncConstants.Groups.Content;

    private readonly IContentService contentService;

    /// <summary>
    ///  Handler constructor Loaded via DI
    /// </summary>
    public ContentTemplateHandler(
        ILogger<ContentTemplateHandler> logger,
        IEntityService entityService,
        IContentService contentService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {

        this.contentService = contentService;

        // make sure we load up the template serializer - because we need that one, not the normal content one.
        this.serializer = syncItemFactory.GetSerializer<IContent>("contentTemplateSerializer") ??
            throw new NullReferenceException("Can not load the contentTemplateSerializer");
    }

    protected override Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key) {
        if (key != Guid.Empty) return Task.FromResult(Enumerable.Empty<IEntity>());
        var blueprints = entityService.GetAll(UmbracoObjectTypes.DocumentBlueprint);
        return Task.FromResult<IEnumerable<IEntity>>(blueprints);
    }

    /// <summary>
    ///  Delete a content template via the ContentService
    /// </summary>
    /// 
    protected override async Task DeleteViaServiceAsync(IContent item)
        => await Task.Run(() => contentService.DeleteBlueprint(item));

    /// <summary>
    ///  Fetch a content template via the ContentService
    /// </summary>
    protected override async Task<IContent?> GetFromServiceAsync(Guid key)
        => await Task.FromResult(contentService.GetBlueprintById(key));

    /// <summary>
    ///  Fetch a content template via the ContentService
    /// </summary>
    protected override Task<IContent?> GetFromServiceAsync(string alias)
        => Task.FromResult<IContent?>(default);

    /// <summary>
    ///  Manage the content blueprint saved notification
    /// </summary>
    public async Task HandleAsync(ContentSavedBlueprintNotification notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;

        var item = notification.SavedBlueprint;
        try
        {
            var handlerFolders = GetDefaultHandlerFolders();
            var attempts = await ExportAsync(item, handlerFolders, DefaultConfig);
            foreach (var attempt in attempts.Where(x => x.Success))
            {
                if (attempt.FileName is null) continue;
                await this.CleanUpAsync(item, attempt.FileName, handlerFolders.Last());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create uSync export file");
            notification.Messages.Add(new EventMessage("uSync", $"Failed to create export file : {ex.Message}", EventMessageType.Warning));
        }
    }

    /// <summary>
    ///  manage the notification when a blueprint is deleted
    /// </summary>
    public async Task HandleAsync(ContentDeletedBlueprintNotification notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;

        foreach (var item in notification.DeletedBlueprints)
        {
            try
            {
                var handlerFolders = GetDefaultHandlerFolders();
                await ExportDeletedItemAsync(item, handlerFolders, DefaultConfig);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create delete marker");
                notification.Messages.Add(new EventMessage("uSync", $"Failed to mark as deleted : {ex.Message}", EventMessageType.Warning));
            }
        }
    }
}
