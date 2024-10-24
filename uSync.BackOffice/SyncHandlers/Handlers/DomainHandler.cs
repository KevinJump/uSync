using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to mange Domain settings for uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.DomainHandler, "Domains", "Domains", uSyncConstants.Priorites.DomainSettings
    , Icon = "icon-home", EntityType = "domain")]
public class DomainHandler : SyncHandlerBase<IDomain>, ISyncHandler,
    INotificationAsyncHandler<SavedNotification<IDomain>>,
    INotificationAsyncHandler<DeletedNotification<IDomain>>
{
    /// <inheritdoc/>
    public override string Group => uSyncConstants.Groups.Content;

    private readonly IDomainService domainService;

    /// <inheritdoc/>
    public DomainHandler(
        ILogger<DomainHandler> logger,
        IEntityService entityService,
        IDomainService domainService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService configService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, configService, syncItemFactory)
    {
        this.domainService = domainService;
    }

    /// <inheritdoc/>
    protected override string GetItemName(IDomain item)
        => item.DomainName;

    /// <inheritdoc/>
    protected override string GetItemPath(IDomain item, bool useGuid, bool isFlat)
        => $"{item.DomainName.ToSafeFileName(shortStringHelper)}_{item.LanguageIsoCode?.ToSafeFileName(shortStringHelper)}";

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key)
    {
        if (key == Guid.Empty) 
            return (await domainService.GetAllAsync(true))
                .Where(x => x is IEntity)
                .Select(x => x as IEntity);

        return await base.GetChildItemsAsync(key);
    }
}
