using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    [SyncHandler("memberTypeHandler", "Member Types", "MemberTypes", uSyncBackOfficeConstants.Priorites.MemberTypes, IsTwoPass = true, Icon = "icon-item-arrangement")]
    public class MemberTypeHandler : SyncHandlerTreeBase<IMemberType, IMemberTypeService>, ISyncHandler
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IMemberTypeService memberTypeService,
            ISyncSerializer<IMemberType> serializer,
            ISyncTracker<IMemberType> tracker,
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, tracker, syncFileService, settings)
        {
            this.memberTypeService = memberTypeService;

            this.itemObjectType = UmbracoObjectTypes.MemberType;

            // this is also set in base, but explity here so you know
            //    no folders for membertypes
            this.itemContainerType = UmbracoObjectTypes.Unknown;

            this.Enabled = false; 
            // turn it off it appears to break things in current build
        }

        protected override void InitializeEvents()
        {
            MemberTypeService.Saved += EventSavedItem;
            MemberTypeService.Deleted += EventDeletedItem;
        }

        protected override void DeleteFolder(int id)
            => memberTypeService.DeleteContainer(id);

        protected override void DeleteViaService(IMemberType item)
            => memberTypeService.Delete(item);

        protected override IMemberType GetFromService(int id)
            => memberTypeService.Get(id);

        protected override IMemberType GetFromService(Guid key)
            => memberTypeService.Get(key);

        protected override IMemberType GetFromService(string alias)
            => memberTypeService.Get(alias);

        protected override string GetItemFileName(IUmbracoEntity item)
            => item.Name;
    }
}
