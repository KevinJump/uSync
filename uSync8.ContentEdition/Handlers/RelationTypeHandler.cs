using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Xml.Linq;

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
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("relationTypeHandler", "Relations",
            "RelationTypes", uSyncBackOfficeConstants.Priorites.RelationTypes,
            Icon = "icon-traffic usync-addon-icon",
            EntityType = UdiEntityType.RelationType, IsTwoPass = false)]
    public class RelationTypeHandler : SyncHandlerBase<IRelationType, IRelationService>, ISyncExtendedHandler
    {
        private readonly IRelationService relationService;

        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        public RelationTypeHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IRelationType> serializer,
            ISyncTracker<IRelationType> tracker,
            ISyncDependencyChecker<IRelationType> checker,
            SyncFileService syncFileService,
            IRelationService relationService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, syncFileService)
        {
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
        ///  Workout if we are excluding this relationType from export/import
        /// </summary>
        protected override bool ShouldExport(XElement node, HandlerSettings config)
        {
            var exclude = config.GetSetting<string>("Exclude", string.Empty);

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
            => useGuid ? item.Key.ToString() : item.Alias.ToSafeAlias();

        protected override void InitializeEvents(HandlerSettings settings)
        {
            RelationService.SavedRelationType += EventSavedItem;
            RelationService.DeletedRelationType += EventDeletedItem;

            if (settings.GetSetting<bool>("IncludeRelations", false))
            {
                // relation saving is noisy, for example if you copy a load of 
                // pages the save event fires a lot. 
                RelationService.SavedRelation += RelationService_SavedRelation;
                RelationService.DeletedRelation += RelationService_DeletedRelation;

                // the lock and timer are used so we don't do multiple saves
                // instead we queue things and when nothing has changed 
                // for about 4 seconds, then we save everything in the queue.
                saveTimer = new Timer(4064);
                saveTimer.Elapsed += SaveTimer_Elapsed;

                pendingSaveIds = new List<int>();
                saveLock = new object();
            }
        }

        private void RelationService_SavedRelation(IRelationService sender, Umbraco.Core.Events.SaveEventArgs<IRelation> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            lock (saveLock)
            {
                saveTimer.Stop();
                saveTimer.Start();

                // add each item to the save list (if we haven't already)
                foreach (var item in e.SavedEntities)
                {
                    if (!pendingSaveIds.Contains(item.RelationTypeId))
                        pendingSaveIds.Add(item.RelationTypeId);
                }
            }
        }

        private void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (saveLock)
            {
                UpdateRelationTypes(pendingSaveIds);
                pendingSaveIds.Clear();
            }
        }

        private static Timer saveTimer;
        private static List<int> pendingSaveIds;
        private static object saveLock;

        private void RelationService_DeletedRelation(IRelationService sender, Umbraco.Core.Events.DeleteEventArgs<IRelation> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            var types = new List<int>();

            foreach (var item in e.DeletedEntities)
            {
                if (!types.Contains(item.RelationTypeId))
                    types.Add(item.RelationTypeId);
            }

            UpdateRelationTypes(types);
        }

        private void UpdateRelationTypes(IEnumerable<int> types)
        {
            foreach (var type in types)
            {
                var relationType = relationService.GetRelationTypeById(type);

                var attempts = Export(relationType, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);

                if (!(this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure))
                {
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        this.CleanUp(relationType, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                    }
                }
            }
        }
    }
}