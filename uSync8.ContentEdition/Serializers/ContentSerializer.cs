﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    [SyncSerializer("5CB57139-8AF7-4813-95AD-C075D74636C2", "ContentSerializer", uSyncConstants.Serialization.Content)]
    public class ContentSerializer : ContentSerializerBase<IContent>, ISyncOptionsSerializer<IContent>
    {
        protected readonly IContentService contentService;
        protected readonly IFileService fileService;

        private bool performDoubleLookup;

        public ContentSerializer(
            IEntityService entityService,
            ILocalizationService localizationService,
            IRelationService relationService,
            ILogger logger,
            IContentService contentService,
            IFileService fileService,
            SyncValueMapperCollection syncMappers)
            : base(entityService, localizationService, relationService, logger, UmbracoObjectTypes.Document, syncMappers)
        {
            this.contentService = contentService;
            this.fileService = fileService;

            this.relationAlias = Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias;

            performDoubleLookup = UmbracoVersion.LocalVersion.Major != 8 || UmbracoVersion.LocalVersion.Minor < 4;
        }

        #region Serialization

        protected override SyncAttempt<XElement> SerializeCore(IContent item, SyncSerializerOptions options)
        {
            var node = InitializeNode(item, item.ContentType.Alias, options);
            var info = SerializeInfo(item, options);
            var properties = SerializeProperties(item, options);

            node.Add(info);
            node.Add(properties);

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContent), ChangeType.Export);
        }

        protected override XElement SerializeInfo(IContent item, SyncSerializerOptions options)
        {
            var info = base.SerializeInfo(item, options);

            info.Add(SerailizePublishedStatus(item, options));
            info.Add(SerializeSchedule(item, options));
            info.Add(SerializeTemplate(item, options));

            return info;
        }

        protected virtual XElement SerializeTemplate(IContent item, SyncSerializerOptions options)
        {
            if (item.TemplateId != null && item.TemplateId.HasValue)
            {
                var template = fileService.GetTemplate(item.TemplateId.Value);
                if (template != null)
                {
                    return new XElement("Template",
                        new XAttribute("Key", template.Key),
                        template.Alias);
                }
            }
            return new XElement("Template");
        }

        private XElement SerailizePublishedStatus(IContent item, SyncSerializerOptions options)
        {
            var activeCultures = options.GetCultures();

            var published = new XElement("Published");
            if (item.AvailableCultures.Count() == 0)
            {
                published.Add(new XAttribute("Default", item.Published));
            }
            else
            {
                foreach (var culture in item.AvailableCultures.OrderBy(x => x))
                {
                    if (activeCultures.IsValid(culture))
                    {
                        published.Add(new XElement("Published", item.IsCulturePublished(culture),
                            new XAttribute("Culture", culture)));
                    }
                }
            }
            return published;
        }

        protected virtual XElement SerializeSchedule(IContent item, SyncSerializerOptions options)
        {
            var node = new XElement("Schedule");
            var schedules = item.ContentSchedule.FullSchedule;
            if (schedules != null)
            {
                foreach (var schedule in schedules.OrderBy(x => x.Action).OrderBy(x => x.Culture))
                {
                    node.Add(new XElement("ContentSchedule",
                        new XElement("Culture", schedule.Culture.ToLower()),
                        new XElement("Action", schedule.Action),
                        new XElement("Date", schedule.Date.ToString("s"))));
                }
            }

            return node;
        }

        #endregion

        #region Deserialization

        protected override SyncAttempt<IContent> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var item = FindOrCreate(node);

            DeserializeBase(item, node, options);
            DeserializeTemplate(item, node);
            DeserializeSchedules(item, node, options);

            return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.Import);
        }

        protected virtual void DeserializeTemplate(IContent item, XElement node)
        {
            var templateNode = node.Element("Info")?.Element("Template");

            if (templateNode != null)
            {
                var alias = templateNode.ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    var template = fileService.GetTemplate(alias);
                    if (template != null)
                    {
                        item.TemplateId = template.Id;
                        return;
                    }
                }

                var key = templateNode.Attribute("Key").ValueOrDefault(Guid.Empty);
                if (key != Guid.Empty)
                {
                    var template = fileService.GetTemplate(key);
                    if (template != null)
                    {
                        item.TemplateId = template.Id;
                        return;
                    }
                }

            }
        }

        private void DeserializeSchedules(IContent item, XElement node, SyncSerializerOptions options)
        {
            var schedules = node.Element("Info")?.Element("Schedule");
            if (schedules != null && schedules.HasElements)
            {
                var currentSchedules = item.ContentSchedule.FullSchedule;
                var nodeSchedules = new List<ContentSchedule>();

                foreach (var schedule in schedules.Elements("ContentSchedule"))
                {
                    var importSchedule = GetContentScheduleFromNode(schedule);

                    if (importSchedule.Date < DateTime.Now) continue; // don't add schedules in the past

                    nodeSchedules.Add(importSchedule);

                    var existing = FindSchedule(currentSchedules, importSchedule);
                    if (existing != null)
                    {
                        item.ContentSchedule.Remove(existing);
                    }
                    item.ContentSchedule.Add(importSchedule);
                }

                // remove things that are in the current but not the import. 

                var toRemove = currentSchedules.Where(x => FindSchedule(nodeSchedules, x) == null);

                foreach (var oldItem in toRemove)
                {
                    item.ContentSchedule.Remove(oldItem);
                }
            }
        }

        public override SyncAttempt<IContent> DeserializeSecondPass(IContent item, XElement node, SyncSerializerOptions options)
        {
            var attempt = DeserializeProperties(item, node, options);
            if (!attempt.Success)
            {
                return SyncAttempt<IContent>.Fail(item.Name, ChangeType.ImportFail, attempt.Exception);
            }

            // sort order
            var sortOrder = node.Element("Info").Element("SortOrder").ValueOrDefault(-1);
            HandleSortOrder(item, sortOrder);


            var trashed = node.Element("Info").Element("Trashed").ValueOrDefault(false);
            HandleTrashedState(item, trashed);

            // published status
            // this does the last save and publish
            var saveAttempt = DoSaveOrPublish(item, node, options);

            if (saveAttempt.Success)
            {
                // setting the 'saved' flag to true on the stops base classes from also saving the item.
                return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.Import, attempt.Status, true);
            }

            return SyncAttempt<IContent>.Fail(item.Name, item, ChangeType.ImportFail, $"{saveAttempt.Result} {attempt.Status}");
        }

        protected override void HandleTrashedState(IContent item, bool trashed)
        {
            if (!trashed && item.Trashed)
            {
                // if the item is trashed, then the change of it's parent 
                // should restore it (as long as we do a move!)
                contentService.Move(item, item.ParentId);
            }
            else if (trashed && !item.Trashed)
            {
                // move to the recycle bin
                contentService.MoveToRecycleBin(item);
            }
        }

        protected virtual Attempt<string> DoSaveOrPublish(IContent item, XElement node, SyncSerializerOptions options)
        {
            var publishedNode = node.Element("Info")?.Element("Published");
            if (publishedNode != null)
            {
                var schedules = GetSchedules(node.Element("Info")?.Element("Schedule"));

                if (publishedNode.HasElements)
                {
                    // culture based publishing.
                    var cultures = options.GetDeserializedCultures(node);
                    var unpublishMissingCultures = cultures.Count > 0;

                    var culturesAreScheduled = schedules.Count(x => cultures.InvariantContains(x.Culture));

                    var cultureStatuses = new Dictionary<string, uSyncContentState>();

                    foreach (var culturePublish in publishedNode.Elements("Published"))
                    {
                        var culture = culturePublish.Attribute("Culture").ValueOrDefault(string.Empty);

                        if (!string.IsNullOrWhiteSpace(culture) && cultures.IsValid(culture))
                        {
                            // is the item published in the config 
                            var state = culturePublish.ValueOrDefault(false) 
                                ? uSyncContentState.Published 
                                : uSyncContentState.Unpublished;

                            // pending or outstanding scheduled actions can change the action we take.
                            cultureStatuses[culture] = CalculateScheduledState(state, schedules, culture);
                        }
                        
                    }

                    if (cultureStatuses.Count > 0)
                    {
                        return PublishItem(item, cultureStatuses, unpublishMissingCultures);
                    }
                }
                else
                {
                    var state = publishedNode.Attribute("Default").ValueOrDefault(false) 
                        ? uSyncContentState.Published 
                        : uSyncContentState.Unpublished;


                    state = CalculateScheduledState(state, schedules, string.Empty);


                    if (state == uSyncContentState.Published)
                    {
                        return PublishItem(item);
                    }
                    else if (state == uSyncContentState.Unpublished && item.Published == true)
                    {
                        contentService.Unpublish(item);
                    }
                }
            }

            this.SaveItem(item);
            return Attempt.Succeed("Saved");
        }

        /// <summary>
        ///  work out what the current status of the item should be. 
        /// </summary>
        public uSyncContentState CalculateScheduledState(uSyncContentState state, IList<ContentSchedule> schedules, string culture)
        {
            foreach (var schedule in schedules.Where(x => x.Culture.InvariantEquals(culture))
                .OrderBy(x => x.Date))
            {
                switch (schedule.Action)
                {
                    case ContentScheduleAction.Release:
                        if (schedule.Date < DateTime.Now)
                        {
                            state = uSyncContentState.Published;
                        }
                        else
                        {
                            // if a schedule publish hasn't happend yet,
                            // if the whole culture is already 'published' we save it.
                            // but if its unpublished, then we keep that, so it will get 
                            // unpublished if it isn't 
                            if (state == uSyncContentState.Published) state = uSyncContentState.Saved;
                        }
                        break;
                    case ContentScheduleAction.Expire:
                        if (schedule.Date < DateTime.Now)
                        {
                            state = uSyncContentState.Unpublished;
                        }
                        break;
                }

            }
            return state;
        }

        
   

        private IList<ContentSchedule> GetSchedules(XElement schedulesNode)
        {
            var schedules = new List<ContentSchedule>();
            if (schedulesNode != null && schedulesNode.HasElements)
            {
                foreach (var schedule in schedulesNode.Elements("ContentSchedule"))
                {
                    schedules.Add(GetContentScheduleFromNode(schedule));
                }
            }
            return schedules;
        }

        private ContentSchedule FindSchedule(IEnumerable<ContentSchedule> currentSchedules, ContentSchedule newSchedule)
        {
            var schedule = currentSchedules.FirstOrDefault(x => x.Culture == newSchedule.Culture && x.Action == newSchedule.Action);
            if (schedule != null) return schedule;

            return null;
        }

        private ContentSchedule GetContentScheduleFromNode(XElement scheduleNode)
        {
            var key = scheduleNode.Attribute("Key").ValueOrDefault(Guid.Empty);
            var culture = scheduleNode.Element("Culture").ValueOrDefault(string.Empty);
            var date = scheduleNode.Element("Date").ValueOrDefault(DateTime.MinValue);
            var action = scheduleNode.Element("Action").ValueOrDefault(ContentScheduleAction.Release);

            return new ContentSchedule(key, culture, date, action);
        }

        /// <summary>
        ///  Publish a content item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Attempt<string> PublishItem(IContent item)
        {
            try
            {
                var result = contentService.SaveAndPublish(item);
                return result.ToAttempt();
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifer, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw ex;
                return Attempt.Succeed($"Published");
            }
        }

        /// <summary>
        ///  Publish/unpublish Specified cultures for an item, and optionally unpublish missing cultures
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cultures"></param>
        /// <param name="unpublishMissing"></param>
        /// <returns></returns>
        private Attempt<string> PublishItem(IContent item, IDictionary<string, uSyncContentState> cultures, bool unpublishMissing)
        {
            if (cultures == null) return PublishItem(item);

            try
            {
                var hasBeenSaved = false; 

                var publishedCultures = cultures
                    .Where(x => x.Value == uSyncContentState.Published)
                    .Select(x => x.Key)
                    .ToArray();

                if (publishedCultures.Length > 0)
                {
                    var result = contentService.SaveAndPublish(item, publishedCultures);

                    // if this fails, we return the result
                    if (!result.Success) return result.ToAttempt();

                    // if its published here it's also saved, so we can skip the save below.
                    hasBeenSaved = true;
                }
                
                var unpublishedCultures = cultures
                    .Where(x => x.Value == uSyncContentState.Unpublished)
                    .Select(x => x.Key)
                    .ToArray();

                if (unpublishedCultures.Length > 0) {
                
                    foreach (var culture in unpublishedCultures)
                    {
                        // unpublish if the culture is currently published.
                        if (item.PublishedCultures.InvariantContains(culture))
                            contentService.Unpublish(item, culture);
                    }
                }

                
                if (unpublishMissing)
                    UnpublishMissingCultures(item, cultures.Select(x => x.Key).ToArray());

                // if we get to this point and no save has been called, we should call it. 
                if (!hasBeenSaved && item.IsDirty())
                    contentService.Save(item);

                return Attempt.Succeed("Done");
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifer, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw ex;
                return Attempt.Succeed($"Published");
            }
        }

        /// <summary>
        ///  Unpublish any cultures that are not explicity mentiond in the culture list.
        /// </summary>
        private void UnpublishMissingCultures(IContent item, string[] allCultures)
        {
            var missingCultures = item
                .PublishedCultures
                .Where(x => !allCultures.InvariantContains(x))
                .ToArray();

            if (missingCultures != null && missingCultures.Length > 0)
            {
                foreach (var culture in missingCultures)
                {
                    logger.Debug<ContentSerializer>("Unpublishing culture not defined in config file");
                    contentService.Unpublish(item, culture);
                }
            }
        }

        #endregion

        protected override IContent CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var parentId = parent != null ? parent.Id : -1;
            return contentService.Create(alias, parentId, itemType);
        }

        #region Finders
        protected override IContent FindItem(int id)
        {
            var item = contentService.GetById(id);
            if (item != null)
            {
                if (!this.nameCache.ContainsKey(id))
                    this.nameCache[id] = new Tuple<Guid, string>(item.Key, item.Name);
                return item;
            }
            return null;
        }

        protected override IContent FindItem(Guid key)
        {
            if (performDoubleLookup)
            {
                // fixed v8.4+ by https://github.com/umbraco/Umbraco-CMS/issues/2997
                var entity = entityService.Get(key);
                if (entity != null)
                    return contentService.GetById(entity.Id);
            }
            else
            {
                return contentService.GetById(key);
            }

            return null;
        }

        protected override IContent FindAtRoot(string alias)
        {
            var rootNodes = contentService.GetRootContent();
            if (rootNodes.Any())
            {
                return rootNodes.FirstOrDefault(x => x.Name.ToSafeAlias().InvariantEquals(alias));
            }

            return null;
        }

        #endregion

        public override void Save(IEnumerable<IContent> items)
            => contentService.Save(items);

        protected override void SaveItem(IContent item)
        {
            try
            {
                contentService.Save(item);
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifer, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw ex;
            }
        }

        protected override void DeleteItem(IContent item)
        {
            try
            {
                contentService.Delete(item);
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifer, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw ex;
            }
        }
    }
}
