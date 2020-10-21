using System;
using System.Collections.Generic;
using System.Linq;
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
    [SyncSerializer("9A5C253C-71FA-4FC0-9B7C-9D0522AAE880", "Domain Serializer", uSyncConstants.Serialization.Domain)]
    public class DomainSerializer : SyncSerializerBase<IDomain>, ISyncNodeSerializer<IDomain>
    {
        private readonly IDomainService domainService;
        private readonly IContentService contentService;
        private readonly ILocalizationService localizationService;

        public DomainSerializer(IEntityService entityService, ILogger logger,
            IDomainService domainService,
            IContentService contentService,
            ILocalizationService localizationService)
            : base(entityService, logger)
        {
            this.domainService = domainService;
            this.contentService = contentService;
            this.localizationService = localizationService;
        }

        protected override SyncAttempt<IDomain> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var item = FindOrCreate(node);

            var info = node?.Element("Info");
            if (info == null)
                return SyncAttempt<IDomain>.Fail(node.GetAlias(), ChangeType.Fail, new ArgumentNullException("Info", "Missing Info Section in XML"));

            var isoCode = info.Element("Language").ValueOrDefault(string.Empty) ?? string.Empty;

            var changes = new List<uSyncChange>();

            if (!string.IsNullOrWhiteSpace(isoCode))
            {
                var language = localizationService.GetLanguageByIsoCode(isoCode);
                if (language != null && item.LanguageId != language.Id)
                {
                    changes.AddUpdate("Id", item.LanguageId, language.Id);
                    item.LanguageId = language.Id;
                }
            }

            var rootItem = default(IContent);

            var rootKey = info.Element("Root")?.Attribute("Key").ValueOrDefault(Guid.Empty) ?? Guid.Empty;
            if (rootKey != Guid.Empty)
            {
                rootItem = contentService.GetById(rootKey);
            }

            if (rootItem == default(IContent))
            {
                var rootName = info.Element("Root").ValueOrDefault(string.Empty);
                if (rootName != string.Empty)
                {
                    rootItem = FindByPath(rootName.ToDelimitedList("/"));
                }
            }

            if (rootItem != default(IContent) && item.RootContentId != rootItem.Id)
            {
                changes.AddUpdate("RootItem", item.RootContentId, rootItem.Id);
                item.RootContentId = rootItem.Id;
            }

            return SyncAttempt<IDomain>.Succeed(item.DomainName, item, ChangeType.Import, changes);

        }

        private IDomain FindOrCreate(XElement node)
        {
            var item = FindItem(node);
            if (item != null) return item;

            return new UmbracoDomain(node.GetAlias());

        }

        protected override SyncAttempt<XElement> SerializeCore(IDomain item, SyncSerializerOptions options)
        {
            var node = new XElement(ItemType,
                new XAttribute("Key", item.Id.ToGuid()),
                new XAttribute("Alias", item.DomainName));

            var info = new XElement("Info",
                new XElement("IsWildcard", item.IsWildcard),
                new XElement("Language", item.LanguageIsoCode));


            if (item.RootContentId.HasValue)
            {
                var rootNode = contentService.GetById(item.RootContentId.Value);

                if (rootNode != null)
                {
                    info.Add(new XElement("Root", GetItemPath(rootNode),
                        new XAttribute("Key", rootNode.Key)));
                }
            }

            node.Add(info);

            return SyncAttempt<XElement>.SucceedIf(
                node != null, item.DomainName, node, typeof(IDomain), ChangeType.Export);
        }

        protected override IDomain FindItem(Guid key)
            => null;

        protected override IDomain FindItem(string alias)
            => domainService.GetByName(alias);


        /// <summary>
        ///  these items do exist in the content serializer, 
        ///  but they are here because we are not doing this
        ///  for our primary object type but for content.
        /// </summary>

        protected virtual string GetItemPath(IContent item)
        {
            var entity = entityService.Get(item.Id);
            return GetItemPath(entity);
        }

        private string GetItemPath(IEntitySlim item)
        {
            var path = "";
            if (item.ParentId != -1)
            {
                var parent = entityService.Get(item.ParentId);
                if (parent != null)
                    path += GetItemPath(parent);
            }

            return path += "/" + item.Name;
        }

        private IContent FindByPath(IEnumerable<string> folders)
        {
            var item = default(IContent);
            foreach (var folder in folders)
            {
                var next = FindContentItem(folder, item);
                if (next == null)
                    return item;

                item = next;
            }

            return item;
        }

        private IContent FindContentItem(string alias, IContent parent)
        {
            if (parent != null)
            {
                var children = entityService.GetChildren(parent.Id, UmbracoObjectTypes.Document);
                var child = children.FirstOrDefault(x => x.Name.InvariantEquals(alias));
                if (child != null)
                    return contentService.GetById(child.Id);
            }

            return default(IContent);
        }


        protected override void SaveItem(IDomain item)
            => domainService.Save(item);

        protected override void DeleteItem(IDomain item)
            => domainService.Delete(item);

        protected override string ItemAlias(IDomain item)
            => item.DomainName;
    }
}
