using System;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Strings;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync.ContentEdition.Handlers
{
    [SyncHandler("contentHandler", "Content", "Content", uSyncBackOfficeConstants.Priorites.Content
        , Icon = "icon-document usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.Document)]
    public class ContentHandler : ContentHandlerBase<IContent, IContentService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IContentService contentService;

        public ContentHandler(
            IShortStringHelper shortStringHelper,
            IContentService contentService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IContent> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.contentService = contentService;
        }
        protected override void DeleteViaService(IContent item)
            => contentService.Delete(item);

        protected override IContent GetFromService(int id)
            => contentService.GetById(id);

        protected override IContent GetFromService(Guid key)
            => contentService.GetById(key);

        protected override IContent GetFromService(string alias)
            => null;

        protected override void InitializeEvents(HandlerSettings settings)
        {
            ContentService.Saved += EventSavedItem;
            ContentService.Deleted += EventDeletedItem;
            ContentService.Moved += EventMovedItem;
            ContentService.Trashed += EventMovedItem;
        }

        public uSyncAction Import(string file)
        {
            var attempt = this.Import(file, DefaultConfig, SerializerFlags.OnePass);
            return uSyncActionHelper<IContent>.SetAction(attempt, file, this.Alias, IsTwoPass);
        }


   }
}
