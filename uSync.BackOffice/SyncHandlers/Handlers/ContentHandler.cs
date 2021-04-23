using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("contentHandler", "Content", "Content", uSyncBackOfficeConstants.Priorites.Content
        , Icon = "icon-document usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.Document)]
    public class ContentHandler : ContentHandlerBase<IContent, IContentService>, ISyncHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IContentService contentService;

        public ContentHandler(
            IContentService contentService,
            IShortStringHelper shortStringHelper,
            ILogger<ContentHandler> logger,
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

        /// <summary>
        ///  Get child items 
        /// </summary>
        /// <remarks>
        ///  The core method works for all services, (using entities) - but if we look up
        ///  the actual type for content and media, we save ourselves an extra lookup later on
        ///  and this speeds up the itteration by quite a bit (onle less db trip per item).
        /// </remarks>
        protected override IEnumerable<IEntity> GetChildItems(IEntity parent)
        {
            if (parent != null)
            {
                var items = new List<IContent>();
                const int pageSize = 5000;
                var page = 0;
                var total = long.MaxValue;
                while (page * pageSize < total)
                {
                    items.AddRange(contentService.GetPagedChildren(parent.Id, page++, pageSize, out total));
                }
                return items;
            }
            else
            {
                return contentService.GetRootContent();
            }
        }


    }
}
