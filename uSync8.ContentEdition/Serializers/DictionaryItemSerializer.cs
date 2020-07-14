using System;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    [SyncSerializer("4D18F4C3-6EBC-4AAD-8D20-6353BDBBD484", "Dicrionary Serializer", uSyncConstants.Serialization.Dictionary)]
    public class DictionaryItemSerializer : SyncSerializerBase<IDictionaryItem>, ISyncOptionsSerializer<IDictionaryItem>
    {
        private readonly ILocalizationService localizationService;

        public DictionaryItemSerializer(IEntityService entityService, ILogger logger,
            ILocalizationService localizationService)
            : base(entityService, logger)
        {
            this.localizationService = localizationService;
        }

        protected override SyncAttempt<IDictionaryItem> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var item = FindItem(node);

            var info = node.Element("Info");
            var alias = node.GetAlias();

            Guid? parentKey = null;
            var parentItemKey = info.Element("Parent").ValueOrDefault(string.Empty);
            if (parentItemKey != string.Empty)
            {
                var parent = localizationService.GetDictionaryItemByKey(parentItemKey);
                if (parent != null)
                {
                    parentKey = parent.Key;
                }
            }

            var key = node.GetKey();

            if (item == null)
            {
                item = new DictionaryItem(parentKey, alias);
                item.Key = key;
            }
            else
            {
                item.ParentId = parentKey;
            }

            if (item.ItemKey != alias)
                item.ItemKey = alias;

            if (item.Key != key)
                item.Key = key;

            DeserializeTranslations(item, node);

            return SyncAttempt<IDictionaryItem>.Succeed(item.ItemKey, item, ChangeType.Import);
        }

        private void DeserializeTranslations(IDictionaryItem item, XElement node)
        {
            var translationNode = node.Element("Translations");
            if (translationNode == null) return;

            var currentTranslations = item.Translations.ToList();

            foreach (var translation in translationNode.Elements("Translation"))
            {
                var language = translation.Attribute("Language").ValueOrDefault(string.Empty);
                if (language == string.Empty) continue;

                var itemTranslation = item.Translations.FirstOrDefault(x => x.Language.IsoCode == language);
                if (itemTranslation != null)
                {
                    itemTranslation.Value = translation.Value;
                }
                else
                {
                    var lang = localizationService.GetLanguageByIsoCode(language);
                    if (lang != null)
                    {
                        currentTranslations.Add(new DictionaryTranslation(lang, translation.Value));
                    }
                }
            }

            item.Translations = currentTranslations;

            // localizationService.Save(item);
        }

        protected override SyncAttempt<XElement> SerializeCore(IDictionaryItem item, SyncSerializerOptions options)
        {
            var node = InitializeBaseNode(item, item.ItemKey, GetLevel(item));

            var info = new XElement("Info");

            if (item.ParentId.HasValue)
            {
                var parent = FindItem(item.ParentId.Value);
                if (parent != null)
                {
                    info.Add(new XElement("Parent", parent.ItemKey));
                }
            }

            var translationsNode = new XElement("Translations");

            foreach (var translation in item.Translations)
            {
                translationsNode.Add(new XElement("Translation", translation.Value,
                    new XAttribute("Language", translation.Language.IsoCode)));
            }

            node.Add(info);
            node.Add(translationsNode);

            return SyncAttempt<XElement>.Succeed(
                item.ItemKey, node, typeof(IDictionaryItem), ChangeType.Export);
        }

        protected override IDictionaryItem FindItem(Guid key)
            => localizationService.GetDictionaryItemById(key);

        protected override IDictionaryItem FindItem(string alias)
            => localizationService.GetDictionaryItemByKey(alias);

        private int GetLevel(IDictionaryItem item, int level = 0)
        {
            if (!item.ParentId.HasValue) return level;

            var parent = FindItem(item.ParentId.Value);
            if (parent != null)
                return GetLevel(parent, level + 1);

            return level;
        }

        protected override void SaveItem(IDictionaryItem item)
            => localizationService.Save(item);

        protected override void DeleteItem(IDictionaryItem item)
            => localizationService.Delete(item);

        protected override string ItemAlias(IDictionaryItem item)
            => item.ItemKey;
    }
}
