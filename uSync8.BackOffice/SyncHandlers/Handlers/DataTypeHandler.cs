using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice.Services;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("dataTypeHandler", "Datatype Handler", "DataTypes", uSyncBackOfficeConstants.Priorites.DataTypes, Icon = "icon-autofill")]
    public class DataTypeHandler : SyncHandlerTreeBase<IDataType, IDataTypeService>, ISyncHandler
    {
        private readonly IDataTypeService dataTypeService;

        public DataTypeHandler(
            IEntityService entityService,
            IDataTypeService dataTypeService,
            IProfilingLogger logger, 
            ISyncSerializer<IDataType> serializer, 
            ISyncTracker<IDataType> tracker,
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, tracker, syncFileService, settings)
        {
            this.dataTypeService = dataTypeService;
        }

        protected override IDataType GetFromService(int id)
            => dataTypeService.GetDataType(id);

        public void InitializeEvents()
        {
            DataTypeService.Saved += ItemSavedEvent;
            DataTypeService.Deleted += ItemDeletedEvent;
        }

        protected override string GetItemFileName(IUmbracoEntity item)
            => item.Name.ToSafeFileName();

    }
}
