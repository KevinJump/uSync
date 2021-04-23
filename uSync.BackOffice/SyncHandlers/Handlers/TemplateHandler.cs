using System.Collections.Generic;
using System.Linq;

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
    [SyncHandler("templateHandler", "Templates", "Templates", uSyncBackOfficeConstants.Priorites.Templates,
        Icon = "icon-layout", EntityType = UdiEntityType.Template, IsTwoPass = true)]
    public class TemplateHandler : SyncHandlerLevelBase<ITemplate, IFileService>, ISyncHandler,
        INotificationHandler<SavedNotification<ITemplate>>,
        INotificationHandler<DeletedNotification<ITemplate>>,
        INotificationHandler<MovedNotification<ITemplate>>
    {
        private readonly IFileService fileService;

        public TemplateHandler(
            IShortStringHelper shortStringHelper,
            ILogger<TemplateHandler> logger,
            uSyncConfigService uSyncConfig,
            IFileService fileService,
            IEntityService entityService,
            AppCaches appCaches,
            ISyncSerializer<ITemplate> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, logger, uSyncConfig, appCaches, serializer, syncItemFactory, syncFileService, entityService)
        {
            this.fileService = fileService;
        }

        protected override string GetItemName(ITemplate item) => item.Name;

        protected override IEnumerable<IEntity> GetChildItems(int parent)
            => fileService.GetTemplates(parent).Where(x => x is IEntity)
            .Select(x => x as IEntity);

        protected override IEnumerable<IEntity> GetFolders(int parent)
            => GetChildItems(parent);

    }
}
