using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to mange Relation types in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.RelationTypeHandler, "Relations",
        "RelationTypes", uSyncConstants.Priorites.RelationTypes,
        Icon = "icon-link",
        EntityType = UdiEntityType.RelationType, IsTwoPass = false)]
public class RelationTypeHandler : SyncHandlerBase<IRelationType>, ISyncHandler,
    INotificationAsyncHandler<SavedNotification<IRelationType>>,
    INotificationAsyncHandler<DeletedNotification<IRelationType>>,
    INotificationAsyncHandler<SavingNotification<IRelationType>>,
    INotificationAsyncHandler<DeletingNotification<IRelationType>>
{
    private readonly IRelationService relationService;

    /// <inheritdoc/>
    public override string Group => uSyncConstants.Groups.Content;

    /// <inheritdoc/>
    public RelationTypeHandler(
        ILogger<RelationTypeHandler> logger,
        IEntityService entityService,
        IRelationService relationService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        this.relationService = relationService;
    }

    /// <summary>
    ///  Relations that by default we exclude, if the exclude setting is used,then it will override these values
    ///  and they will be included if not explicitly set;
    /// </summary>
    private const string defaultRelations = "relateParentDocumentOnDelete,relateParentMediaFolderOnDelete,relateDocumentOnCopy,umbMedia,umbDocument";

    /// <inheritdoc/>
    protected override Task<bool> ShouldExportAsync(XElement node, HandlerSettings config)
    {
        var exclude = config.GetSetting<string>("Exclude", defaultRelations);

        if (!string.IsNullOrWhiteSpace(exclude) && exclude.Contains(node.GetAlias()))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override Task<bool> ShouldImportAsync(XElement node, HandlerSettings config)
        => ShouldExportAsync(node, config);

    /// <inheritdoc/>
    protected override string GetItemName(IRelationType item)
        => item.Name ?? item.Alias;

    /// <inheritdoc/>
    protected override string GetItemFileName(IRelationType item)
        => GetItemAlias(item).ToSafeAlias(shortStringHelper);

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key)
        => key == Guid.Empty
            ? await Task.FromResult(relationService.GetAllRelationTypes())
            : [];
}
