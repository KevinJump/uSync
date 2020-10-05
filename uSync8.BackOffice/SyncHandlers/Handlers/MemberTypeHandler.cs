using System;

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
    [SyncHandler("memberTypeHandler", "Member Types", "MemberTypes", uSyncBackOfficeConstants.Priorites.MemberTypes,
        IsTwoPass = true, Icon = "icon-users", EntityType = UdiEntityType.MemberType)]
    public class MemberTypeHandler : SyncHandlerContainerBase<IMemberType, IMemberTypeService>, ISyncExtendedHandler
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeHandler(
            IMemberTypeService memberTypeService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IMemberType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.memberTypeService = memberTypeService;
        }


        [Obsolete("Use constructors with collections")]
        protected MemberTypeHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IMemberTypeService memberTypeService,
            ISyncSerializer<IMemberType> serializer,
            ISyncTracker<IMemberType> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<IMemberType> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, syncFileService)
        {
            this.memberTypeService = memberTypeService;

            this.Enabled = false;
            // turn it off it appears to break things in current build
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
                return memberType.Alias.ToSafeFileName();
            }

            return item.Name.ToSafeFileName();
        }

        protected override string GetItemAlias(IMemberType item)
            => item.Alias;
    }

}
