using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Implement;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("relationTypeHandler", "Relations",
            "RelationTypes", uSyncBackOfficeConstants.Priorites.RelationTypes,
            Icon = "icon-traffic usync-addon-icon",
            EntityType = UdiEntityType.RelationType, IsTwoPass = false)]
    public class RelationTypeHandler : SyncHandlerBase<IRelationType, IRelationService>, ISyncExtendedHandler, ISyncItemHandler
    {
        private readonly IRelationService relationService;
        private readonly IShortStringHelper shortStringHelper;

        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        public RelationTypeHandler(
            IShortStringHelper shortStringHelper,
            ILogger<RelationTypeHandler> logger,
            uSyncConfigService uSyncConfigService,
            AppCaches appCaches,
            ISyncSerializer<IRelationType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService,
            IRelationService relationService,
            IEntityService entityService)
            : base(logger, uSyncConfigService, appCaches, serializer, syncItemFactory, syncFileService, entityService)
        {
            this.shortStringHelper = shortStringHelper;
            this.relationService = relationService;
        }

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

        protected override bool ShouldImport(XElement node, HandlerSettings config)
            => ShouldExport(node, config);



        protected override void DeleteViaService(IRelationType item)
            => relationService.Delete(item);

        protected override IRelationType GetFromService(int id)
            => relationService.GetRelationTypeById(id);

        protected override IRelationType GetFromService(Guid key)
            => relationService.GetRelationTypeById(key);

        protected override IRelationType GetFromService(string alias)
            => relationService.GetRelationTypeByAlias(alias);

        protected override string GetItemName(IRelationType item)
            => item.Name;

        protected override string GetItemPath(IRelationType item, bool useGuid, bool isFlat)
            => useGuid ? item.Key.ToString() : item.Alias.ToSafeAlias(shortStringHelper);

        
        
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