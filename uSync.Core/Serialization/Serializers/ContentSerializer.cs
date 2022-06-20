using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Mapping;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("5CB57139-8AF7-4813-95AD-C075D74636C2", "ContentSerializer", uSyncConstants.Serialization.Content)]
    public class ContentSerializer : ContentSerializerBase<IContent>, ISyncSerializer<IContent>
    {
        protected readonly IContentService contentService;
        protected readonly IFileService fileService;

        public ContentSerializer(
            IEntityService entityService,
            ILocalizationService localizationService,
            IRelationService relationService,
            IShortStringHelper shortStringHelper,
            ILogger<ContentSerializer> logger,
            IContentService contentService,
            IFileService fileService,
            SyncValueMapperCollection syncMappers)
            : base(entityService, localizationService, relationService, shortStringHelper, logger, UmbracoObjectTypes.Document, syncMappers)
        {
            this.contentService = contentService;
            this.fileService = fileService;

            this.relationAlias = Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias;
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
            // get the list of cultures we are serializing from the config
            var activeCultures = options.GetCultures();

            var published = new XElement("Published");

            // to make this a non-breaking change, we say default = item.published, but when 
            // dealing with cultures it isn't used. 
            published.Add(new XAttribute("Default", item.Published));

            foreach (var culture in item.AvailableCultures.OrderBy(x => x))
            {
                if (activeCultures.IsValid(culture))
                {
                    published.Add(new XElement("Published", item.IsCulturePublished(culture),
                        new XAttribute("Culture", culture)));
                }
            }
            return published;
        }

        protected virtual XElement SerializeSchedule(IContent item, SyncSerializerOptions options)
        {
            var node = new XElement("Schedule");
            var schedules = contentService.GetContentScheduleByContentId(item.Id);

            var cultures = options.GetCultures();

            if (schedules != null)
            {
                foreach (var schedule in schedules.FullSchedule
                    .OrderBy(x => x.Action.ToString())
                    .ThenBy(x => x.Culture))
                {

                    // only export if its a blank culture or one of the ones we have set. 
                    if (cultures.IsValidOrBlank(schedule.Culture))
                    {
                        node.Add(new XElement("ContentSchedule",
                            // new XAttribute("Key", schedule.Id),
                            new XElement("Culture", schedule.Culture),
                            new XElement("Action", schedule.Action),
                            new XElement("Date", schedule.Date.ToString("s"))));
                    }
                }
            }

            return node;
        }

        #endregion

        #region Deserialization

        protected override SyncAttempt<IContent> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var attempt = FindOrCreate(node);
            if (!attempt.Success) throw attempt.Exception;

            var item = attempt.Result;

            var details = new List<uSyncChange>();

            details.AddRange(DeserializeBase(item, node, options));

            if (node.Element("Info") != null)
            {
                var trashed = node.Element("Info").Element("Trashed").ValueOrDefault(false);
                details.AddNotNull(HandleTrashedState(item, trashed));
            }

            details.AddNotNull(DeserializeTemplate(item, node));

            var propertiesAttempt = DeserializeProperties(item, node, options);
            if (!propertiesAttempt.Success)
            {
                return SyncAttempt<IContent>.Fail(item.Name, item, ChangeType.ImportFail, "Failed to deserialize properties", attempt.Exception);
            }

            details.AddRange(propertiesAttempt.Result);

            // sort order
            var sortOrder = node.Element("Info").Element("SortOrder").ValueOrDefault(-1);
            details.AddNotNull(HandleSortOrder(item, sortOrder));

            var publishTimer = Stopwatch.StartNew();


            if (details.HasWarning() && options.FailOnWarnings())
            {
                // Fail on warning. means we don't save or publish because something is wrong ?
                return SyncAttempt<IContent>.Fail(item.Name, item, ChangeType.ImportFail, "Failed with warnings", details,
                    new Exception("Import failed because of warnings, and fail on warnings is true"));
            }

            // published status
            // this does the last save and publish
            var saveAttempt = DoSaveOrPublish(item, node, options);
            if (saveAttempt.Success)
            {
                var message = saveAttempt.Result;

                if (details.Any(x => x.Change == ChangeDetailType.Warning))
                    message += $" with warning(s)"; 

                if (publishTimer.ElapsedMilliseconds > 10000)
                {
                    message += $" (Slow publish {publishTimer.ElapsedMilliseconds}ms)";
                }

                var changeType = options.GetSetting(uSyncConstants.DefaultSettings.OnlyPublishDirty, uSyncConstants.DefaultSettings.OnlyPublishDirty_Default) && !item.IsDirty() 
                    ? ChangeType.NoChange : ChangeType.Import;

                // we say no change back, this stops the core second pass function from saving 
                // this item (which we have just done with DoSaveOrPublish)
                return SyncAttempt<IContent>.Succeed(item.Name, item, changeType, message, true, details);
            }
            else
            {
                return SyncAttempt<IContent>.Fail(item.Name, item, ChangeType.ImportFail, saveAttempt.Result, saveAttempt.Exception);
            }
        }

        protected virtual uSyncChange DeserializeTemplate(IContent item, XElement node) 
        { 
            var templateNode = node.Element("Info")?.Element("Template");

            if (templateNode != null)
            {
                var alias = templateNode.ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    var template = fileService.GetTemplate(alias);
                    if (template != null && template.Id != item.TemplateId)
                    {
                        var oldValue = item.TemplateId;
                        item.TemplateId = template.Id;
                        return uSyncChange.Update("Template", "Template", oldValue, template.Id);
                    }
                }

                var key = templateNode.ValueOrDefault(Guid.Empty);
                if (key != Guid.Empty)
                {
                    var template = fileService.GetTemplate(key);
                    if (template != null && template.Id != item.TemplateId)
                    {
                        var oldValue = item.TemplateId;
                        item.TemplateId = template.Id;
                        return uSyncChange.Update("Template", "Template", oldValue, template.Id);
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///  Second pass. 
        /// </summary>
        /// <remarks>
        ///  in v9.4 we eliminated this - but because schedules now require the content to 
        ///  exist, we need to second pass schedules, only. 99% of the time this shouldn't
        ///  have any impact. 
        /// </remarks>
        public override SyncAttempt<IContent> DeserializeSecondPass(IContent item, XElement node, SyncSerializerOptions options)
        {
            var changes = DeserializeSchedules(item, node, options);  
            if (changes.Any())
                return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.Import, "", true, changes.ToList());

            return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.NoChange, null);
        }

        private IEnumerable<uSyncChange> DeserializeSchedules(IContent item, XElement node, SyncSerializerOptions options)
        {
            logger.LogDebug("Deserialize Schedules");

            var changes = new List<uSyncChange>();
            var nodeSchedules = new ContentScheduleCollection();
            var currentSchedules = contentService.GetContentScheduleByContentId(item.Id);
            var cultures = options.GetDeserializedCultures(node);

            var schedules = node.Element("Info")?.Element("Schedule");
            if (schedules != null && schedules.HasElements)
            {

                foreach (var schedule in schedules.Elements("ContentSchedule"))
                {
                    var importSchedule = GetContentScheduleFromNode(schedule);
                    if (cultures.IsValidOrBlank(importSchedule.Culture))
                    {
                        if (importSchedule.Date < DateTime.Now)
                            continue; // don't add schedules in the past

                        logger.LogDebug("Adding {action} {culture} {date}", importSchedule.Action, importSchedule.Culture, importSchedule.Date);
                        nodeSchedules.Add(importSchedule);

                        var existing = FindSchedule(currentSchedules, importSchedule);
                        if (existing != null)
                        {
                            currentSchedules.Remove(existing);
                        }
                        currentSchedules.Add(importSchedule);
                        changes.Add(uSyncChange.Update("Schedule", $"{importSchedule.Culture} {importSchedule.Action}", "", importSchedule.Date.ToString()));
                    }
                }
            }

            if (currentSchedules != null)
            {
                // remove things that are in the current but not the import. 

                var toRemove = currentSchedules.FullSchedule.Where(x => FindSchedule(nodeSchedules, x) == null);

                foreach (var oldItem in toRemove)
                {
                    if (cultures.IsValidOrBlank(oldItem.Culture))
                    {
                        logger.LogDebug("Removing Schedule : {culture} {action} {date}", oldItem.Culture, oldItem.Action, oldItem.Date);
                        // only remove a culture if this seralization included it. 
                        // we don't remove things we didn't serialize. 
                        currentSchedules.Remove(oldItem);

                        changes.Add(uSyncChange.Delete("Schedule", $"{oldItem.Culture} - {oldItem.Action}", oldItem.Date.ToString()));
                    }
                }

                if (changes.Any())
                {
                    contentService.PersistContentSchedule(item, currentSchedules);
                    return changes;
                }

                return Enumerable.Empty<uSyncChange>();
            }


            return Enumerable.Empty<uSyncChange>();
        }

        private ContentSchedule GetContentScheduleFromNode(XElement scheduleNode)
        {
            var key = Guid.Empty;
            var culture = scheduleNode.Element("Culture").ValueOrDefault(string.Empty);
            var date = scheduleNode.Element("Date").ValueOrDefault(DateTime.MinValue);
            var action = scheduleNode.Element("Action").ValueOrDefault(ContentScheduleAction.Release);

            return new ContentSchedule(key, culture, date, action);
        }

        private ContentSchedule FindSchedule(ContentScheduleCollection currentSchedules, ContentSchedule newSchedule)
        {
            var schedule = currentSchedules.GetSchedule(newSchedule.Culture, newSchedule.Action);
            if (schedule != null && schedule.Any()) return schedule.FirstOrDefault();

            return null;
        }

  
        protected override uSyncChange HandleTrashedState(IContent item, bool trashed)
        {
            if (!trashed && item.Trashed)
            {
                // if the item is trashed, then the change of it's parent 
                // should restore it (as long as we do a move!)

                contentService.Move(item, item.ParentId);

                // clean out any relations for this item (some versions of Umbraco don't do this on a Move)
                CleanRelations(item, "relateParentDocumentOnDelete");

                return uSyncChange.Update("Restored", item.Name, "Recycle Bin", item.ParentId.ToString());

            }
            else if (trashed && !item.Trashed)
            {

                // clean any relations that may be there (stops an error)
                CleanRelations(item, "relateParentDocumentOnDelete");

                // move to the recycle bin
                contentService.MoveToRecycleBin(item);
                return uSyncChange.Update("Moved to Bin", item.Name, "", "Recycle Bin");
            }

            return null;
        }

        protected virtual Attempt<string> DoSaveOrPublish(IContent item, XElement node, SyncSerializerOptions options)
        {
            if (options.GetSetting(uSyncConstants.DefaultSettings.OnlyPublishDirty, uSyncConstants.DefaultSettings.OnlyPublishDirty_Default) && !item.IsDirty())
            {
                logger.LogDebug("{name} not publishing because nothing is dirty [{dirty} {userDirty}]", item.Name, item.IsDirty(), item.IsAnyUserPropertyDirty());
                return Attempt.Succeed("No Changes");
            }

            var publishedNode = node.Element("Info")?.Element("Published");
            if (!item.Trashed && publishedNode != null)
            {
                var schedules = GetSchedules(node.Element("Info")?.Element("Schedule"));

                if (publishedNode.HasElements)
                {
                    // culture based publishing.
                    var cultures = options.GetDeserializedCultures(node);

                    // Only unpublish other cultures, when we are not already filtered by cultures
                    // this stops things we don't care about this time being unpublished.
                    var unpublishMissingCultures = cultures.Count == 0;

                    var cultureStatuses = new Dictionary<string, uSyncContentState>();

                    foreach (var culturePublish in publishedNode.Elements("Published"))
                    {
                        var culture = culturePublish.Attribute("Culture").ValueOrDefault(string.Empty);

                        if (!string.IsNullOrWhiteSpace(culture) && cultures.IsValid(culture))
                        {
                            // is the item published in the config file
                            var configState = culturePublish.ValueOrDefault(false)
                                ? uSyncContentState.Published
                                : uSyncContentState.Unpublished;

                            // pending or outstanding scheduled actions can change the action we take.
                            cultureStatuses[culture] =
                                schedules.CalculateCultureState(culture, configState);
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

                    state = schedules.CalculateCultureState(string.Empty, state);

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
        ///  work out what the current status of a given culture should be. 
        /// </summary>

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

        public Attempt<string> PublishItem(IContent item)
        {
            try
            {
                var result = contentService.SaveAndPublish(item);
                if (!result.Success)
                {
                    var messages = result.EventMessages.FormatMessages(",");
                    logger.LogError("Failed to publish {result} : {messages}", result.Result, messages);
                }
                return result.ToAttempt();
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifer, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw;
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

                if (unpublishedCultures.Length > 0)
                {

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
                if (!ex.Message.Contains("siteUri")) throw;
                return Attempt.Succeed($"Published");
            }
        }
        /// <summary>
        ///  unpublish any cultures that are marked as published, in umbraco but are not published
        ///  in our *.config file.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="publishedCultures"></param>
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
                    logger.LogDebug("Unpublishing culture not defined in config file {culture}", culture);
                    contentService.Unpublish(item, culture);
                }
            }
        }

        #endregion

        protected override Attempt<IContent> CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var parentId = parent != null ? parent.Id : -1;
            var item = contentService.Create(alias, parentId, itemType);
            if (item == null)
                return Attempt.Fail(item, new ArgumentException($"Unable to create content item of type {itemType}"));

            return Attempt.Succeed(item);
        }

        #region Finders
        public override IContent FindItem(int id)
        {
            var item = contentService.GetById(id);
            if (item != null)
            {
                AddToNameCache(id, item.Key, item.Name);
                return item;
            }
            return null;
        }

        public override IContent FindItem(Guid key)
            => contentService.GetById(key);

        protected override IContent FindAtRoot(string alias)
        {
            var rootNodes = contentService.GetRootContent();
            if (rootNodes.Any())
            {
                return rootNodes.FirstOrDefault(x => x.Name.ToSafeAlias(shortStringHelper).InvariantEquals(alias));
            }

            return null;
        }

        #endregion

        public override void Save(IEnumerable<IContent> items)
            => contentService.Save(items);

        public override void SaveItem(IContent item)
        {
            try
            {
                contentService.Save(item);
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifer, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw;
            }
        }

        public override void DeleteItem(IContent item)
        {
            try
            {
                contentService.Delete(item);
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifer, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw;
            }
        }
    }
}
