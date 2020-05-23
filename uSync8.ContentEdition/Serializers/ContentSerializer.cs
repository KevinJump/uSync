using System;
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
    public class ContentSerializer : ContentSerializerBase<IContent>, ISyncSerializer<IContent>
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

        protected override SyncAttempt<XElement> SerializeCore(IContent item)
        {
            var node = InitializeNode(item, item.ContentType.Alias);

            var info = SerializeInfo(item);

            var properties = SerializeProperties(item);

            node.Add(info);
            node.Add(properties);

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContent), ChangeType.Export);
        }

        protected override XElement SerializeInfo(IContent item)
        {
            var info = base.SerializeInfo(item);

            info.Add(SerailizePublishedStatus(item));
            info.Add(SerializeSchedule(item));
            info.Add(SerializeTemplate(item));

            return info;
        }

        protected virtual XElement SerializeTemplate(IContent item)
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

        private XElement SerailizePublishedStatus(IContent item)
        {
            var published = new XElement("Published", new XAttribute("Default", item.Published));
            foreach (var culture in item.AvailableCultures.OrderBy(x => x))
            {
                published.Add(new XElement("Published", item.IsCulturePublished(culture),
                    new XAttribute("Culture", culture)));
            }
            return published;
        }

        private XElement SerializeSchedule(IContent item)
        {
            var node = new XElement("Schedule");
            var schedules = item.ContentSchedule.FullSchedule;
            if (schedules != null)
            {
                foreach (var schedule in schedules.OrderBy(x => x.Id))
                {
                    node.Add(new XElement("ContentSchedule",
                        new XAttribute("Key", schedule.Id),
                        new XElement("Culture", schedule.Culture),
                        new XElement("Action", schedule.Action),
                        new XElement("Date", schedule.Date)));
                }
            }

            return node;
        }

        #endregion

        #region Deserialization

        protected override SyncAttempt<IContent> DeserializeCore(XElement node)
        {
         
            var item = FindOrCreate(node);

            DeserializeBase(item, node);
            DeserializeTemplate(item, node);

            return SyncAttempt<IContent>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }

        protected virtual void DeserializeTemplate(IContent item, XElement node)
        {
            var templateNode = node.Element("Info")?.Element("Template");

            if (templateNode != null)
            {
                var key = templateNode.ValueOrDefault(Guid.Empty);
                if (key != Guid.Empty)
                {
                    var template = fileService.GetTemplate(key);
                    if (template != null)
                    {
                        item.TemplateId = template.Id;
                        return;
                    }
                }

                var alias = templateNode.ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    var template = fileService.GetTemplate(alias);
                    if (template != null)
                        item.TemplateId = template.Id;
                }
            }
        }


        public override SyncAttempt<IContent> DeserializeSecondPass(IContent item, XElement node, SerializerFlags flags)
        {
            var attempt = DeserializeProperties(item, node);
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
            var saveAttempt = DoSaveOrPublish(item, node);

            if (saveAttempt.Success)
            {
                // we say no change back, this stops the core second pass function from saving 
                // this item (which we have just done with DoSaveOrPublish)
                return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.NoChange, attempt.Status);
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

        protected virtual Attempt<string> DoSaveOrPublish(IContent item, XElement node)
        {
            var publishedNode = node.Element("Info")?.Element("Published");
            if (publishedNode != null)
            {
                if (publishedNode.HasElements)
                {
                    // culture based publishing.
                    var publishedCultures = new List<string>();
                    foreach (var culturePublish in publishedNode.Elements("Published"))
                    {
                        var culture = culturePublish.Attribute("Culture").ValueOrDefault(string.Empty);
                        var status = culturePublish.ValueOrDefault(false);

                        if (!string.IsNullOrWhiteSpace(culture) && status)
                        {
                            publishedCultures.Add(culture);
                        }
                    }

                    if (publishedCultures.Count > 0)
                    {
                        return PublishItem(item, publishedCultures.ToArray());
                    }
                }
                else
                {
                    // default publish the lot. 
                    if (publishedNode.Attribute("Default").ValueOrDefault(false))
                    {
                        return PublishItem(item, null);
                    }
                    else if (item.Published)
                    {
                        // unpublish
                        contentService.Unpublish(item);
                    }
                }
            }

            // if we get here, save 
            /*
            var result = contentService.Save(item);
            if (result.Success) */

            this.SaveItem(item);
            return Attempt.Succeed("Saved");

            // return Attempt.Fail("Save Failed " + result.EventMessages);
        }

        private Attempt<string> PublishItem(IContent item, string[] cultures)
        {
            try
            {
                PublishResult result;
                if (cultures != null)
                {
                    result = contentService.SaveAndPublish(item, cultures);
                    UnpublishCultures(item, cultures);
                }
                else
                {
                    result = contentService.SaveAndPublish(item);
                }

                if (result.Success)
                    return Attempt.Succeed("Published");

                var messages = "";
                if (result.EventMessages.Count > 0) 
                {
                    messages = string.Join(": ", 
                        result.EventMessages.GetAll().Select(x => $"{x.Category}: {x.Message}"));
                }

                return Attempt.Fail($"Publish Failed : {messages}<br/>");
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
        ///  unpublish any cultures that are marked as published, in umbraco but are not published
        ///  in our *.config file.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="publishedCultures"></param>
        private void UnpublishCultures(IContent item, string[] publishedCultures)
        {
            var cultures = item.PublishedCultures.Where(x => !publishedCultures.InvariantContains(x)).ToArray();
            if (cultures != null && cultures.Length > 0)
            {
                foreach (var culture in cultures)
                {
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
