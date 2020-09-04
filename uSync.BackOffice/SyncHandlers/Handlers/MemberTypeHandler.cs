using System;

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
    [SyncHandler("memberTypeHandler", "Member Types", "MemberTypes", uSyncBackOfficeConstants.Priorites.MemberTypes, 
        IsTwoPass = true, Icon = "icon-users", EntityType = UdiEntityType.MemberType)]
    public class MemberTypeHandler : SyncHandlerContainerBase<IMemberType, IMemberTypeService>, ISyncExtendedHandler
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeHandler(
            IShortStringHelper shortStringHelper,
            IMemberTypeService memberTypeService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IMemberType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.memberTypeService = memberTypeService;
        }


        protected override void InitializeEvents(HandlerSettings settings)
        {
            MemberTypeService.Saved += EventSavedItem;
            MemberTypeService.Deleted += EventDeletedItem;
            MemberTypeService.Moved += EventMovedItem;

            MemberTypeService.SavedContainer += EventContainerSaved;
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


        protected override IEntity GetContainer(int id)
            => memberTypeService.GetContainer(id);

        protected override IEntity GetContainer(Guid key)
            => memberTypeService.GetContainer(key);

        protected override string GetItemFileName(IUmbracoEntity item, bool useGuid)
        {
            if (useGuid) return item.Key.ToString();

            if (item is IMemberType memberType)
            {
                return memberType.Alias.ToSafeFileName(shortStringHelper);
            }

            return item.Name.ToSafeFileName(shortStringHelper);
        }

        protected override string GetItemAlias(IMemberType item)
            => item.Alias;
    }

}
