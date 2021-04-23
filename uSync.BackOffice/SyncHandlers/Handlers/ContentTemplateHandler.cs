using System;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("contentTemplateHandler", "Blueprints", "Blueprints", uSyncBackOfficeConstants.Priorites.ContentTemplate
        , Icon = "icon-document-dashed-line usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.DocumentBlueprint)]
    public class ContentTemplateHandler : ContentHandlerBase<IContent, IContentService>, ISyncHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IContentService contentService;

        public ContentTemplateHandler(
            IContentService contentService,
            IShortStringHelper shortStringHelper,
            ILogger<ContentTemplateHandler> logger,
            uSyncConfigService uSyncConfigService, 
            AppCaches appCaches,
            ISyncSerializer<IContent> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService,
            IEntityService entityService)
            : base(shortStringHelper, logger, uSyncConfigService, appCaches, serializer, syncItemFactory, syncFileService, entityService)
        {
            this.contentService = contentService;
        }

        protected override void DeleteViaService(IContent item)
            => contentService.DeleteBlueprint(item);

        protected override IContent GetFromService(int id)
            => contentService.GetBlueprintById(id);

        protected override IContent GetFromService(Guid key)
            => contentService.GetBlueprintById(key);

        protected override IContent GetFromService(string alias)
            => null;
    }
}
