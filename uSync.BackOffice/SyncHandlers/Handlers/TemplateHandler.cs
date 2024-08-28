using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
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
    /// <summary>
    ///  Handler for Template items in Umbraco
    /// </summary>
    [SyncHandler(uSyncConstants.Handlers.TemplateHandler, "Templates", "Templates", uSyncConstants.Priorites.Templates,
        Icon = "icon-layout", EntityType = UdiEntityType.Template, IsTwoPass = true)]
    public class TemplateHandler : SyncHandlerLevelBase<ITemplate, IFileService>, ISyncHandler, ISyncPostImportHandler,
        INotificationHandler<SavedNotification<ITemplate>>,
        INotificationHandler<DeletedNotification<ITemplate>>,
        INotificationHandler<MovedNotification<ITemplate>>,
        INotificationHandler<SavingNotification<ITemplate>>,
        INotificationHandler<DeletingNotification<ITemplate>>,
        INotificationHandler<MovingNotification<ITemplate>>
    {
        private readonly IFileService fileService;

        /// <inheritdoc/>
        public TemplateHandler(
            ILogger<TemplateHandler> logger,
            IEntityService entityService,
            IFileService fileService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
        {
            this.fileService = fileService;
        }

        /// <inheritdoc/>
        public IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            if (actions == null || !actions.Any())
                return Enumerable.Empty<uSyncAction>();

            var results = new List<uSyncAction>();

            // we only do deletes here. 
            foreach (var action in actions.Where(x => x.Change == ChangeType.Hidden))
            {
                results.AddRange(
                    Import(action.FileName, config, SerializerFlags.LastPass));
            }

            return results;
        }

        /// <inheritdoc/>
        protected override string GetItemName(ITemplate item) => item.Name;

        /// <inheritdoc/>
        protected override IEnumerable<IEntity> GetChildItems(int parent)
            => fileService.GetTemplates(parent).Where(x => x is IEntity)
            .Select(x => x as IEntity);

        /// <inheritdoc/>
        protected override IEnumerable<IEntity> GetFolders(int parent)
            => GetChildItems(parent);

        /// <inheritdoc/>
        protected override string GetItemPath(ITemplate item, bool useGuid, bool isFlat)
            => useGuid ? item.Key.ToString() : item.Alias.ToSafeFileName(shortStringHelper);
    }
}
