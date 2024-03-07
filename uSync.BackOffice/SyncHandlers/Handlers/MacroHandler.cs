//using Microsoft.Extensions.Logging;

//using System.Collections.Generic;
//using System.Linq;

//using Umbraco.Cms.Core.Cache;
//using Umbraco.Cms.Core.Events;
//using Umbraco.Cms.Core.Models;
//using Umbraco.Cms.Core.Models.Entities;
//using Umbraco.Cms.Core.Notifications;
//using Umbraco.Cms.Core.Services;
//using Umbraco.Cms.Core.Strings;
//using Umbraco.Extensions;

//using uSync.BackOffice.Configuration;
//using uSync.BackOffice.Services;
//using uSync.Core;

//using static Umbraco.Cms.Core.Constants;

//namespace uSync.BackOffice.SyncHandlers.Handlers
//{
//    /// <summary>
//    ///  Handler to mange Macros in uSync
//    /// </summary>
//    [SyncHandler(uSyncConstants.Handlers.MacroHandler, "Macros", "Macros", uSyncConstants.Priorites.Macros,
//        Icon = "icon-settings-alt", EntityType = UdiEntityType.Macro)]
//    public class MacroHandler : SyncHandlerBase<IMacro, IMacroService>, ISyncHandler,
//        INotificationHandler<SavedNotification<IMacro>>,
//        INotificationHandler<DeletedNotification<IMacro>>,
//        INotificationHandler<SavingNotification<IMacro>>,
//        INotificationHandler<DeletingNotification<IMacro>>
//    {
//        private readonly IMacroService macroService;

//        /// <inheritdoc/>
//        public MacroHandler(
//            ILogger<MacroHandler> logger,
//            IEntityService entityService,
//            IMacroService macroService,
//            AppCaches appCaches,
//            IShortStringHelper shortStringHelper,
//            SyncFileService syncFileService,
//            uSyncEventService mutexService,
//            uSyncConfigService uSyncConfig,
//            ISyncItemFactory syncItemFactory)
//            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
//        {
//            this.macroService = macroService;
//        }

//        /// <summary>
//        ///  overrider the default export, because macros, don't exist as an object type???
//        /// </summary>
//        public override IEnumerable<uSyncAction> ExportAll(int parent, string folder, HandlerSettings config, SyncUpdateCallback callback)
//        {
//            // we clean the folder out on an export all. 
//            syncFileService.CleanFolder(folder);

//            var actions = new List<uSyncAction>();

//            var items = macroService.GetAll().ToList();
//            int count = 0;
//            foreach (var item in items)
//            {
//                count++;
//                callback?.Invoke(item.Name, count, items.Count);
//                actions.AddRange(Export(item, folder, config));
//            }

//            return actions;
//        }

//        /// <inheritdoc/>
//        protected override string GetItemName(IMacro item)
//            => item.Name;

//        /// <inheritdoc/>
//        protected override string GetItemFileName(IMacro item)
//            => GetItemAlias(item).ToSafeAlias(shortStringHelper);

//        /// <inheritdoc/>
//        protected override IEnumerable<IEntity> GetChildItems(int parent)
//        {
//            if (parent == -1)
//            {
//                return macroService.GetAll().Where(x => x is IEntity)
//                    .Select(x => x as IEntity);
//            }

//            return Enumerable.Empty<IEntity>();
//        }

//    }

//}
