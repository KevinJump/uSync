using System;
using System.Linq;
using System.Xml.Linq;

using Examine;

using NPoco.Expressions;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.ContentEdition.Serializers;
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("contentTemplateHandler", "Blueprints", "Blueprints", uSyncBackOfficeConstants.Priorites.ContentTemplate
        , Icon = "icon-document-dashed-line usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.DocumentBlueprint)]
    public class ContentTemplateHandler : ContentHandlerBase<IContent, IContentService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IContentService contentService;

        public ContentTemplateHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ContentTemplateSerializer serializer, // concrete because we want to make sure we get the blueprint one.
            SyncTrackerCollection trackers,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService,
            IContentService contentService)
            : base(entityService, logger, appCaches, serializer, trackers, checkers, syncFileService)
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

        /// <summary>
        ///  certain packages (DocTypeGridEditior) use temp ContentTemplates as a validation 
        ///  we don't want to sync them.
        /// </summary>
        private static readonly string[] ignoreTemplates = new string[]
        {
            "dtge temp"
        };

        private void ContentService_DeletedBlueprint(IContentService sender, Umbraco.Core.Events.DeleteEventArgs<IContent> e)
        {
            foreach(var item in e.DeletedEntities.Where(x => !IsIgnoredTemplate(x.Name)))
            { 
                EventDeletedItem(sender, e);
            }
        }

        private void ContentService_SavedBlueprint(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            foreach(var item in e.SavedEntities.Where(x => !IsIgnoredTemplate(x.Name)))
            { 
                EventSavedItem(sender, e);
            }
        }

        private bool IsIgnoredTemplate(string name)
            => ignoreTemplates.Any(x => name.InvariantStartsWith(x));
    }
}
