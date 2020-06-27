using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("templateHandler", "Templates", "Templates", uSyncBackOfficeConstants.Priorites.Templates,
        Icon = "icon-layout", EntityType = UdiEntityType.Template, IsTwoPass = true)]
    public class TemplateHandler : SyncHandlerLevelBase<ITemplate>, ISyncExtendedHandler
    {
        private readonly IFileService fileService;

        public TemplateHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            AppCaches appCaches,
            ISyncSerializer<ITemplate> serializer, 
            SyncTrackerCollection trackers,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService,
            IFileService fileService)
            : base(entityService, logger, appCaches, serializer, trackers, checkers, syncFileService)
        {
            this.fileService = fileService;
        }


        protected override ITemplate GetFromService(int id)
            => fileService.GetTemplate(id);

        protected override void InitializeEvents(HandlerSettings settings)
        {
            FileService.SavedTemplate += EventSavedItem;
            FileService.DeletedTemplate += EventDeletedItem;
        }

        protected override string GetItemPath(ITemplate item, bool useGuid, bool isFlat)
            => useGuid ? item.Key.ToString() : item.Alias.ToSafeFileName();

        protected override ITemplate GetFromService(Guid key)
            => fileService.GetTemplate(key);

        protected override ITemplate GetFromService(string alias)
            => fileService.GetTemplate(alias);

        protected override void DeleteViaService(ITemplate item)
            => fileService.DeleteTemplate(item.Alias);

        protected override string GetItemName(ITemplate item)
            => item.Name;
        protected override string GetItemAlias(ITemplate item)
            => item.Alias;


        protected override IEnumerable<IEntity> GetChildItems(int parent)
            => fileService.GetTemplates(parent).Where(x => x is IEntity)
            .Select(x => x as IEntity);

        protected override IEnumerable<IEntity> GetFolders(int parent)
            => GetChildItems(parent);

    }
}
