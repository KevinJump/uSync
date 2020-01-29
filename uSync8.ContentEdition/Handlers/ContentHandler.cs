using System;

using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("contentHandler", "Content", "Content", uSyncBackOfficeConstants.Priorites.Content
        , Icon = "icon-document usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.Document)]
    public class ContentHandler : ContentHandlerBase<IContent, IContentService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IContentService contentService;
        private bool performDoubleLookup = true;

        public ContentHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IContentService contentService,
            ISyncSerializer<IContent> serializer,
            ISyncTracker<IContent> tracker,
            ISyncDependencyChecker<IContent> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, checker, syncFileService)
        {
            this.contentService = contentService;

            performDoubleLookup = UmbracoVersion.LocalVersion.Major != 8 || UmbracoVersion.LocalVersion.Minor < 4;

        }

        protected override void DeleteViaService(IContent item)
            => contentService.Delete(item);

        protected override IContent GetFromService(int id)
            => contentService.GetById(id);

        protected override IContent GetFromService(Guid key)
        {
            if (performDoubleLookup)
            {
                // FIX: alpha bug - getby key is not always uptodate 
                var entity = entityService.Get(key);
                if (entity != null)
                    return contentService.GetById(entity.Id);

                return null;
            }
            else
            {
                return contentService.GetById(key);
            }
        }

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
