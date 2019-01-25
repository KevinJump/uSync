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

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("memberTypeHandler", "Member Type Handler", "MemberTypes", uSyncBackOfficeConstants.Priorites.MemberTypes, IsTwoPass = true)]
    public class MemberTypeHandler : SyncHandlerEntityBase<IMemberType, IMemberTypeService>, ISyncHandler
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IMemberTypeService memberTypeService,
            ISyncSerializer<IMemberType> serializer, 
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) : base(entityService, logger
            , serializer, syncFileService, settings)
        {
            this.memberTypeService = memberTypeService;

            this.itemObjectType = UmbracoObjectTypes.MemberType;

            // this is also set in base, but explity here so you know
            //    no folders for membertypes
            this.itemContainerType = UmbracoObjectTypes.Unknown;

            this.Enabled = false; 
            // turn it off it appears to break things in current build
        }

        public override uSyncAction ReportItem(string file)
        {
            return uSyncAction.Fail("not implimented", typeof(IMemberType), new Exception("Not implimented"));
        }

        public void InitializeEvents()
        {
            MemberTypeService.Saved += ItemSavedEvent;
            MemberTypeService.Deleted += ItemDeletedEvent;
        }

        protected override IMemberType GetFromService(int id)
            => memberTypeService.Get(id);

        protected override string GetItemFileName(IUmbracoEntity item)
            => item.Name;
    }
}
