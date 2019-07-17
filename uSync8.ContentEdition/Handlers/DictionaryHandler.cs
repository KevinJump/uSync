using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("dictionaryHandler", "Dictionary", "Dictionary", uSyncBackOfficeConstants.Priorites.DictionaryItems
        , Icon = "icon-book-alt usync-addon-icon")]
    public class DictionaryHandler : SyncHandlerBase<IDictionaryItem, ILocalizationService>, ISyncHandler
    {
        public string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly ILocalizationService localizationService;

        public DictionaryHandler(IEntityService entityService,
            IProfilingLogger logger, 
            ILocalizationService localizationService,
            ISyncSerializer<IDictionaryItem> serializer,
            ISyncTracker<IDictionaryItem> tracker,
            SyncFileService syncFileService) 
            : base(entityService, logger, serializer, tracker,  syncFileService)
        {
            this.localizationService = localizationService;

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
            foreach(var item in items)
            {
                count++;
                callback?.Invoke(item.ItemKey, count, items.Count);

                actions.Add(Export(item, folder, config));
                actions.AddRange(ExportAll(item.Key, folder, config, callback));
            }

            return actions;
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
    }
}
