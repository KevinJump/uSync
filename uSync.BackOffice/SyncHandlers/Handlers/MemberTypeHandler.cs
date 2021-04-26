using System;

using Microsoft.Extensions.Logging;

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
    [SyncHandler("memberTypeHandler", "Member Types", "MemberTypes", uSyncBackOfficeConstants.Priorites.MemberTypes,
        IsTwoPass = true, Icon = "icon-users", EntityType = UdiEntityType.MemberType)]
    public class MemberTypeHandler : SyncHandlerContainerBase<IMemberType, IMemberTypeService>, ISyncHandler,
        INotificationHandler<SavedNotification<IMemberType>>,
        INotificationHandler<MovedNotification<IMemberType>>,
        INotificationHandler<DeletedNotification<IMemberType>>
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeHandler(
            ILogger<MemberTypeHandler> logger,
            IEntityService entityService,
            IMemberTypeService memberTypeService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncMutexService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncSerializer<IMemberType> serializer,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, serializer, syncItemFactory)
        {
            this.memberTypeService = memberTypeService;
        }
        protected override void DeleteFolder(int id)
            => memberTypeService.DeleteContainer(id);

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
    }

}
