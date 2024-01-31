using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    /// <summary>
    ///  Handler to mange Relation types in uSync
    /// </summary>
    [SyncHandler(uSyncConstants.Handlers.RelationTypeHandler, "Relations",
            "RelationTypes", uSyncConstants.Priorites.RelationTypes,
            Icon = "icon-traffic",
            EntityType = UdiEntityType.RelationType, IsTwoPass = false)]
    public class RelationTypeHandler : SyncHandlerBase<IRelationType, IRelationService>, ISyncHandler,
        INotificationHandler<SavedNotification<IRelationType>>,
        INotificationHandler<DeletedNotification<IRelationType>>,
        INotificationHandler<SavingNotification<IRelationType>>,
        INotificationHandler<DeletingNotification<IRelationType>>
    {
        private readonly IRelationService relationService;

        /// <inheritdoc/>
        public override string Group => uSyncConstants.Groups.Content;

        /// <inheritdoc/>
        public RelationTypeHandler(
            ILogger<RelationTypeHandler> logger,
            IEntityService entityService,
            IRelationService relationService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfigService,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
        {
            this.relationService = relationService;
        }

        /// <inheritdoc/>
        public override IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            var items = relationService.GetAllRelationTypes().ToList();

            foreach (var item in items.Select((relationType, index) => new { relationType, index }))
            {
                callback?.Invoke(item.relationType.Name, item.index, items.Count);
                actions.AddRange(Export(item.relationType, folder, config));
            }

            return actions;
        }


        /// <summary>
        ///  Relations that by default we exclude, if the exlude setting is used,then it will override these values
        ///  and they will be included if not explicity set;
        /// </summary>
        private const string defaultRelations = "relateParentDocumentOnDelete,relateParentMediaFolderOnDelete,relateDocumentOnCopy,umbMedia,umbDocument";

        /// <summary>
        ///  Workout if we are excluding this relationType from export/import
        /// </summary>
        protected override bool ShouldExport(XElement node, HandlerSettings config)
        {
            var exclude = config.GetSetting<string>("Exclude", defaultRelations);

            if (!string.IsNullOrWhiteSpace(exclude) && exclude.Contains(node.GetAlias()))
                return false;

            return true;
        }

        /// <inheritdoc/>
        protected override bool ShouldImport(XElement node, HandlerSettings config)
            => ShouldExport(node, config);


        /// <inheritdoc/>
        protected override string GetItemName(IRelationType item)
            => item.Name;

        /// <inheritdoc/>
        protected override string GetItemFileName(IRelationType item)
            => GetItemAlias(item).ToSafeAlias(shortStringHelper);

        //     private void RelationService_SavedRelation(IRelationService sender, Umbraco.Core.Events.SaveEventArgs<IRelation> e)
        //     {
        //         if (uSync8BackOffice.eventsPaused) return;

        //         lock (saveLock)
        //         {
        //             saveTimer.Stop();
        //             saveTimer.Start();

        //             // add each item to the save list (if we haven't already)
        //             foreach (var item in e.SavedEntities)
        //             {
        //                 if (!pendingSaveIds.Contains(item.RelationTypeId))
        //                     pendingSaveIds.Add(item.RelationTypeId);
        //             }
        //         }
        //     }

        //     private void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        //     {
        //         lock (saveLock)
        //         {
        //             UpdateRelationTypes(pendingSaveIds);
        //             pendingSaveIds.Clear();
        //         }
        //     }

        //     private static Timer saveTimer;
        //     private static List<int> pendingSaveIds;
        //     private static object saveLock;

        //     private void RelationService_DeletedRelation(IRelationService sender, Umbraco.Core.Events.DeleteEventArgs<IRelation> e)
        //     {
        //         if (uSync8BackOffice.eventsPaused) return;

        //         var types = new List<int>();

        //         foreach (var item in e.DeletedEntities)
        //         {
        //             if (!types.Contains(item.RelationTypeId))
        //                 types.Add(item.RelationTypeId);
        //         }

        //         UpdateRelationTypes(types);
        //     }

        //     private void UpdateRelationTypes(IEnumerable<int> types)
        //     {
        //         foreach (var type in types)
        //         {
        //             var relationType = relationService.GetRelationTypeById(type);

        //             var attempts = Export(relationType, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);

        //             if (!(this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure))
        //             {
        //                 foreach (var attempt in attempts.Where(x => x.Success))
        //                 {
        //                     this.CleanUp(relationType, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
        //                 }
        //             }
        //         }
        //     }
    }
}