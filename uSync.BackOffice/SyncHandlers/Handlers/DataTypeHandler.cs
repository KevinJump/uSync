using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
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
    [SyncHandler("dataTypeHandler", "Datatypes", "DataTypes", uSyncBackOfficeConstants.Priorites.DataTypes,
        Icon = "icon-autofill", EntityType = UdiEntityType.DataType)]
    public class DataTypeHandler : SyncHandlerContainerBase<IDataType, IDataTypeService>, ISyncExtendedHandler, ISyncPostImportHandler, ISyncItemHandler,
        INotificationHandler<SavedNotification<IDataType>>,
        INotificationHandler<MovedNotification<IDataType>>,
        INotificationHandler<DeletedNotification<IDataType>>,
        INotificationHandler<EntityContainerSavedNotification>
    {
        private readonly IDataTypeService dataTypeService;


        public DataTypeHandler(
            IShortStringHelper shortStringHelper,
            ILogger<DataTypeHandler> logger,
            uSyncConfigService uSyncConfig,
            IDataTypeService dataTypeService,
            IEntityService entityService,
            AppCaches appCaches,
            ISyncSerializer<IDataType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, logger, uSyncConfig, appCaches, serializer, syncItemFactory, syncFileService, entityService)
        {
            this.dataTypeService = dataTypeService;
        }

        protected override IDataType GetFromService(int id)
            => dataTypeService.GetDataType(id);

        //protected override void InitializeEvents(HandlerSettings settings)
        //{
        //    //DataTypeService.Saved += EventSavedItem;
        //    //DataTypeService.Deleted += EventDeletedItem;
        //    //DataTypeService.Moved += EventMovedItem;
        //    //DataTypeService.SavedContainer += EventContainerSaved;
        //}

        //protected override void TerminateEvents(HandlerSettings settings)
        //{
        //    //DataTypeService.Saved -= EventSavedItem;
        //    //DataTypeService.Deleted -= EventDeletedItem;
        //    //DataTypeService.Moved -= EventMovedItem;
        //    //DataTypeService.SavedContainer -= EventContainerSaved;
        //}

        protected override string GetItemFileName(IUmbracoEntity item, bool useGuid)
        {
            if (useGuid) return item.Key.ToString();
            return item.Name.ToSafeAlias(shortStringHelper);
        }

        protected override void DeleteFolder(int id)
            => dataTypeService.DeleteContainer(id);

        public override IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            if (actions == null || !actions.Any())
                return null;

            foreach (var action in actions)
            {
                var result = Import(action.FileName, config, SerializerFlags.None);
                foreach (var attempt in result)
                {
                    if (attempt.Success && attempt.Item is IDataType dataType)
                    {
                        ImportSecondPass(action.FileName, dataType, config, null);
                    }
                }
            }

            return CleanFolders(folder, -1);
        }

        protected override IDataType GetFromService(Guid key)
            => dataTypeService.GetDataType(key);

        protected override IDataType GetFromService(string alias)
            => dataTypeService.GetDataType(alias);

        protected override void DeleteViaService(IDataType item)
            => dataTypeService.Delete(item);

        protected override IEntity GetContainer(int id)
            => dataTypeService.GetContainer(id);

        protected override IEntity GetContainer(Guid key)
            => dataTypeService.GetContainer(key);
    }
}
