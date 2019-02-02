﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
{
    public class ContentHandler : SyncHandlerTreeBase<IContent, IContentService>, ISyncHandler
    {
        private readonly IContentService contentService;

        public ContentHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            IContentService contentService,
            ISyncSerializer<IContent> serializer, 
            ISyncTracker<IContent> tracker, 
            SyncFileService syncFileService) 
            : base(entityService, logger, serializer, tracker, syncFileService)
        {
            this.contentService = contentService;

            this.itemObjectType = UmbracoObjectTypes.Document;

        }

        protected override void DeleteFolder(int id)
        {
            throw new NotImplementedException();
        }

        protected override void DeleteViaService(IContent item)
            => contentService.Delete(item);

        protected override IContent GetFromService(int id)
            => contentService.GetById(id);

        protected override IContent GetFromService(Guid key)
            => contentService.GetById(key);

        protected override IContent GetFromService(string alias)
        {
            throw new NotImplementedException();
        }

        protected override void InitializeEvents(HandlerSettings settings)
        {
            ContentService.Saved += EventSavedItem;
            ContentService.Deleted += EventDeletedItem;
        }
    }
}
