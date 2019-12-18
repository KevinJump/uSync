using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("dictionaryHandler", "Dictionary", "Dictionary", uSyncBackOfficeConstants.Priorites.DictionaryItems
        , Icon = "icon-book-alt usync-addon-icon", EntityType = UdiEntityType.DictionaryItem)]
    public class DictionaryHandler : SyncHandlerBase<IDictionaryItem, ILocalizationService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly ILocalizationService localizationService;

        public DictionaryHandler(IEntityService entityService,
            IProfilingLogger logger,
            ILocalizationService localizationService,
            ISyncSerializer<IDictionaryItem> serializer,
            ISyncTracker<IDictionaryItem> tracker,
            ISyncDependencyChecker<IDictionaryItem> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, checker, syncFileService)
        {
            this.localizationService = localizationService;
        }

        public override SyncAttempt<IDictionaryItem> Import(string filePath, HandlerSettings config, SerializerFlags flags)
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
                    return SyncAttempt<IDictionaryItem>.Succeed(item.ItemKey, ChangeType.NoChange);
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

        protected override void DeleteViaService(IDictionaryItem item)
            => localizationService.Delete(item);

        protected override IDictionaryItem GetFromService(int id)
            => localizationService.GetDictionaryItemById(id);

        protected override IDictionaryItem GetFromService(Guid key)
            => localizationService.GetDictionaryItemById(key);

        protected override IDictionaryItem GetFromService(string alias)
            => localizationService.GetDictionaryItemByKey(alias);

        protected override string GetItemName(IDictionaryItem item)
            => item.ItemKey;

        protected override string GetItemPath(IDictionaryItem item, bool useGuid, bool isFlat)
            => item.ItemKey.ToSafeFileName();

        protected override void InitializeEvents(HandlerSettings settings)
        {
            LocalizationService.SavedDictionaryItem += EventSavedItem;
            LocalizationService.DeletedDictionaryItem += EventDeletedItem;
        }

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
