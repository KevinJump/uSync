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
    [SyncHandler("memberTypeHandler", "Member Type Handler", "MemberTypes", 3, IsTwoPass = true)]
    public class MemberTypeHandler : SyncHandlerEntityBase<IMemberType>, ISyncHandler
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
        }

        public override uSyncAction ReportItem(string file)
        {
            return uSyncAction.Fail("not implimented", typeof(IMemberType), new Exception("Not implimented"));
        }

        public void InitializeEvents()
        {
            MemberTypeService.Saved += MemberTypeService_Saved;
            MemberTypeService.Deleted += MemberTypeService_Deleted;
        }

        private void MemberTypeService_Deleted(IMemberTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IMemberType> e)
        {
            // no
        }

        private void MemberTypeService_Saved(IMemberTypeService sender, Umbraco.Core.Events.SaveEventArgs<IMemberType> e)
        {
            foreach(var item in e.SavedEntities)
            {
                Export(item, this.DefaultFolder);
            }
        }

        protected override IMemberType GetFromService(int id)
            => memberTypeService.Get(id);

        protected override string GetItemFileName(IUmbracoEntity item)
            => item.Name;
    }
}
