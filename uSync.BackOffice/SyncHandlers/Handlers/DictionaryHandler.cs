using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
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
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    /// <summary>
    ///  Handler to manage Dictionary items via uSync 
    /// </summary>
    [SyncHandler(uSyncConstants.Handlers.DictionaryHandler, "Dictionary", "Dictionary", uSyncConstants.Priorites.DictionaryItems
        , Icon = "icon-book-alt usync-addon-icon", EntityType = UdiEntityType.DictionaryItem)]
    public class DictionaryHandler : SyncHandlerLevelBase<IDictionaryItem, ILocalizationService>, ISyncHandler,
        INotificationHandler<SavedNotification<IDictionaryItem>>,
        INotificationHandler<DeletedNotification<IDictionaryItem>>,
        INotificationHandler<SavingNotification<IDictionaryItem>>,
        INotificationHandler<DeletingNotification<IDictionaryItem>>
    {
        /// <summary>
        ///  Dictionary items belong to the content group by default
        /// </summary>
        public override string Group => uSyncConstants.Groups.Content;

        private readonly ILocalizationService localizationService;

        /// <inheritdoc/>
        public DictionaryHandler(
            ILogger<DictionaryHandler> logger,
            IEntityService entityService,
            ILocalizationService localizationService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfigService,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
        {
            this.localizationService = localizationService;
        }
       
        /// <inheritdoc/>
        protected override IEnumerable<IEntity> GetFolders(int parent)
            => GetChildItems(parent);

        /// <inheritdoc/>
        protected override IEnumerable<IEntity> GetChildItems(int parent)
        {
            if (parent == -1)
            {
                return localizationService.GetRootDictionaryItems()
                    .Where(x => x is IEntity)
                    .Select(x => x as IEntity);
            }
            else
            {
                var item = localizationService.GetDictionaryItemById(parent);
                if (item != null)
                    return localizationService.GetDictionaryItemChildren(item.Key);
            }

            return Enumerable.Empty<IEntity>();
        }

        /// <inheritdoc/>
        protected override string GetItemName(IDictionaryItem item)
            => item.ItemKey;

        /// <inheritdoc/>
        protected override string GetItemPath(IDictionaryItem item, bool useGuid, bool isFlat)
            => item.ItemKey.ToSafeFileName(shortStringHelper);
    }
}
