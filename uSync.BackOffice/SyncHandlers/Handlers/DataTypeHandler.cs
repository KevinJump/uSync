using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("dataTypeHandler", "Datatypes", "DataTypes", uSyncBackOfficeConstants.Priorites.DataTypes,
        Icon = "icon-autofill", EntityType = UdiEntityType.DataType)]
    public class DataTypeHandler : SyncHandlerContainerBase<IDataType, IDataTypeService>, ISyncExtendedHandler, ISyncPostImportHandler
    {
        private readonly IDataTypeService dataTypeService;


        public DataTypeHandler(
            IShortStringHelper shortStringHelper,
            IDataTypeService dataTypeService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IDataType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.dataTypeService = dataTypeService;
        }

        protected override IDataType GetFromService(int id)
            => dataTypeService.GetDataType(id);

        protected override void InitializeEvents(HandlerSettings settings)
        {
            DataTypeService.Saved += EventSavedItem;
            DataTypeService.Deleted += EventDeletedItem;
            DataTypeService.Moved += EventMovedItem;

            DataTypeService.SavedContainer += EventContainerSaved;
        }

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
                var attempt = Import(action.FileName, config, SerializerFlags.None);
                if (attempt.Success)
                {
                    ImportSecondPass(action.FileName, attempt.Item, config, null);
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
