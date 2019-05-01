﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
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

        public ContentSerializer(
            IEntityService entityService, ILogger logger,
            IContentService contentService)
            : base(entityService, logger, UmbracoObjectTypes.Document)
        {
            this.contentService = contentService;
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

            return info;
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
            if (item.Trashed)
            {
                // TODO: Where has changed trashed state gone?
            }

            var name = node.Name.LocalName;
            if (name != string.Empty)
                item.Name = name;

            DeserializeBase(item, node);

            // contentService.Save(item);

            return SyncAttempt<IContent>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }


        public override SyncAttempt<IContent> DeserializeSecondPass(IContent item, XElement node, SerializerFlags flags)
        {
            DeserializeProperties(item, node);

            // sort order
            var sortOrder = node.Element("Info").Element("SortOrder").ValueOrDefault(-1);
            if (sortOrder != -1)
            {
                item.SortOrder = sortOrder;
            }

            // published status
            // this does the last save and publish
            if (DoSaveOrPublish(item, node))
            {
                return SyncAttempt<IContent>.Succeed(item.Name, ChangeType.Import);
            }

            return SyncAttempt<IContent>.Fail(item.Name, ChangeType.ImportFail, "");
            // second pass, is when we do the publish and stuff.
        }

        protected virtual Attempt<string> DoSaveOrPublish(IContent item, XElement node)
        {
            var info = node.Element("Info");

            var published = info.Element("Published")?.Attribute("Default").ValueOrDefault(false) ?? false;

            if (published)
            {
                var publishResult = contentService.SaveAndPublish(item);
                if (publishResult.Success)
                    return Attempt.Succeed("Published");

                return Attempt.Fail("Publish Failed " + publishResult.EventMessages);
            }
            else
            {
                var result = contentService.Save(item);
                if (result.Success)
                    return Attempt.Succeed("Saved");

                return Attempt.Fail("Save Failed " + result.EventMessages);
            }

            // TODO: Culture based publishing

        }

        #endregion

        protected override IContent CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var parentId = parent != null ? parent.Id : -1;
            var item = contentService.Create(alias, parentId, itemType);
            return item;
        }

        #region Finders
        protected override IContent FindItem(int id)
            => contentService.GetById(id);

        protected override IContent FindItem(Guid key)
        {
            // TODO: Alpha version bug, the key isn sometimes an old version
            var entity = entityService.Get(key);
            if (entity != null)
                return contentService.GetById(entity.Id);

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
            => contentService.Save(item);

    }
}
