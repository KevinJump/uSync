using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("dataTypeHandler", "Datatypes", "DataTypes", uSyncBackOfficeConstants.Priorites.DataTypes,
        Icon = "icon-autofill", EntityType = UdiEntityType.DataType)]
    public class DataTypeHandler : SyncHandlerContainerBase<IDataType, IDataTypeService>, ISyncHandler, ISyncPostImportHandler, 
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

        protected override IEntity GetContainer(int id)
            => dataTypeService.GetContainer(id);

        protected override IEntity GetContainer(Guid key)
            => dataTypeService.GetContainer(key);

        protected override void DeleteFolder(int id)
            => dataTypeService.DeleteContainer(id);
    }
}
