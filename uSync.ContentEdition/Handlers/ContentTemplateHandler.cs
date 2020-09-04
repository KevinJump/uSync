using System;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Strings;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.ContentEdition.Serializers;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync.ContentEdition.Handlers
{
    [SyncHandler("contentTemplateHandler", "Blueprints", "Blueprints", uSyncBackOfficeConstants.Priorites.ContentTemplate
        , Icon = "icon-document-dashed-line usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.DocumentBlueprint)]
    public class ContentTemplateHandler : ContentHandlerBase<IContent, IContentService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IContentService contentService;

        public ContentTemplateHandler(
            IShortStringHelper shortStringHelper,
            IContentService contentService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ContentTemplateSerializer serializer, // concreate because we want to make sure we get the blueprint one.
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
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

        protected override void InitializeEvents(HandlerSettings settings)
        {
            ContentService.SavedBlueprint += ContentService_SavedBlueprint;
            ContentService.DeletedBlueprint += ContentService_DeletedBlueprint;
        }

        private void ContentService_DeletedBlueprint(IContentService sender, Umbraco.Core.Events.DeleteEventArgs<IContent> e)
        {
            if (e.DeletedEntities.Any(x => !x.Name.InvariantStartsWith("dtge temp")))
            {
                EventDeletedItem(sender, e);
            }
        }

        private void ContentService_SavedBlueprint(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            // was is a dtge temp file ?
            if (e.SavedEntities.Any(x => !x.Name.InvariantStartsWith("dtge temp")))
            {
                EventSavedItem(sender, e);
            }
        }
    }
}
