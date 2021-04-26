using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
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
    [SyncHandler("dictionaryHandler", "Dictionary", "Dictionary", uSyncConstants.Priorites.DictionaryItems
        , Icon = "icon-book-alt usync-addon-icon", EntityType = UdiEntityType.DictionaryItem)]
    public class DictionaryHandler : SyncHandlerLevelBase<IDictionaryItem, ILocalizationService>, ISyncHandler
    {
        public override string Group => uSyncConstants.Groups.Content;

        private readonly ILocalizationService localizationService;

        public DictionaryHandler(
            ILogger<DictionaryHandler> logger,
            IEntityService entityService,
            ILocalizationService localizationService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncMutexService mutexService,
            uSyncConfigService uSyncConfigService,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
        {
            this.localizationService = localizationService;
        }

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
                    return uSyncAction.SetAction(true, item.ItemKey, change: ChangeType.NoChange).AsEnumerableOfOne() ;
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

        public override IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            syncFileService.CleanFolder(folder);
            return ExportAll(Guid.Empty, folder, config, callback);
        }

        /// <summary>
        ///  don't think you can get dictionary items via the entity service :( 
        /// </summary>
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

        protected override IEnumerable<IEntity> GetFolders(int parent)
            => GetChildItems(parent);

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
        
        protected override string GetItemName(IDictionaryItem item)
            => item.ItemKey;

        protected override string GetItemPath(IDictionaryItem item, bool useGuid, bool isFlat)
            => item.ItemKey.ToSafeFileName(shortStringHelper);

        protected override IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings config)
        {
            if (config != null && IsOneWay(config))
            {
                // if we find it then there is no change. 
                var item = GetExistingItem(filename);
                if (item != null)
                {
                    return uSyncActionHelper<IDictionaryItem>
                        .ReportAction(false, item.ItemKey, "Existing Item will not be overwritten")
                        .AsEnumerableOfOne<uSyncAction>();
                }
            }

            return base.ReportElement(node, filename, config);
        }

        private bool IsOneWay(HandlerSettings config)
        {
            return (config.Settings.ContainsKey("OneWay") && config.Settings["OneWay"].InvariantEquals("true"));
        }
    }
}
