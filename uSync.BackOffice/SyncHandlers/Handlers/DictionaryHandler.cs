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
        public override IEnumerable<uSyncAction> Import(string filePath, HandlerSettings config, SerializerFlags flags)
        {
            if (IsOneWay(config))
            {
                // only sync dictionary items if they are new
                // so if it already exists we don't do the sync

                //
                // <Handler Alias="dictionaryHandler" Enabled="true">
                //    <Add Key="OneWay" Value="true" />
                // </Handler>
                //
                var item = GetExistingItem(filePath);
                if (item != null)
                {
                    return uSyncAction.SetAction(true, item.ItemKey, change: ChangeType.NoChange).AsEnumerableOfOne();
                }
            }

            return base.Import(filePath, config, flags);

        }

        private IDictionaryItem GetExistingItem(string filePath)
        {
            syncFileService.EnsureFileExists(filePath);

            using (var stream = syncFileService.OpenRead(filePath))
            {
                var node = XElement.Load(stream);
                return serializer.FindItem(node);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            syncFileService.CleanFolder(folder);
            return ExportAll(Guid.Empty, folder, config, callback);
        }

        /// <summary>
        ///  Export all Dictionary items based on a parent GUID value
        /// </summary>
        /// <remarks>
        ///  You can't fetch dictionary items via the entity service so they require their own 
        ///  export method. 
        /// </remarks>
        public IEnumerable<uSyncAction> ExportAll(Guid parent, string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            var items = new List<IDictionaryItem>();

            if (parent == Guid.Empty)
            {
                items = localizationService.GetRootDictionaryItems().ToList();
            }
            else
            {
                items = localizationService.GetDictionaryItemChildren(parent).ToList();
            }

            int count = 0;
            foreach (var item in items)
            {
                count++;
                callback?.Invoke(item.ItemKey, count, items.Count);

                actions.AddRange(Export(item, folder, config));
                actions.AddRange(ExportAll(item.Key, folder, config, callback));
            }

            return actions;
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

        /// <inheritdoc/>
        protected override IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings config)
        {
            if (config != null && IsOneWay(config))
            {
                // if we find it then there is no change. 
                var item = GetExistingItem(filename);
                if (item != null)
                {
                    return uSyncActionHelper<IDictionaryItem>
                        .ReportAction(ChangeType.NoChange, item.ItemKey, node.GetPath(), syncFileService.GetSiteRelativePath(filename), item.Key, this.Alias, "Existing Item will not be overwritten")
                        .AsEnumerableOfOne<uSyncAction>();
                }
            }

            return base.ReportElement(node, filename, config);
        }

        private bool IsOneWay(HandlerSettings config)
            => config.GetSetting("OneWay", false);
    }
}
