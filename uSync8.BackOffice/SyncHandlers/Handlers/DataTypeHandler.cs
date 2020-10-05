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

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("dataTypeHandler", "Datatypes", "DataTypes", uSyncBackOfficeConstants.Priorites.DataTypes,
        Icon = "icon-autofill", EntityType = UdiEntityType.DataType)]
    public class DataTypeHandler : SyncHandlerContainerBase<IDataType, IDataTypeService>, ISyncExtendedHandler, ISyncPostImportHandler
    {
        private readonly IDataTypeService dataTypeService;


        public DataTypeHandler(
            IDataTypeService dataTypeService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IDataType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.dataTypeService = dataTypeService;
        }


        [Obsolete("Use constructors with collections")]
        protected DataTypeHandler(
            IEntityService entityService,
            IDataTypeService dataTypeService,
            IProfilingLogger logger,
            ISyncSerializer<IDataType> serializer,
            ISyncDependencyChecker<IDataType> checker,
            ISyncTracker<IDataType> tracker,
            AppCaches appCaches,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, syncFileService)
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
            return item.Name.ToSafeAlias();
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
