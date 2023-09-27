using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("4D18F4C3-6EBC-4AAD-8D20-6353BDBBD484", "Dicrionary Serializer", uSyncConstants.Serialization.Dictionary)]
    public class DictionaryItemSerializer : SyncSerializerBase<IDictionaryItem>, ISyncSerializer<IDictionaryItem>
    {
        private readonly ILocalizationService _localizationService;

        public DictionaryItemSerializer(IEntityService entityService, ILogger<DictionaryItemSerializer> logger,
            ILocalizationService localizationService)
            : base(entityService, logger)
        {
            this._localizationService = localizationService;
        }

        protected override SyncAttempt<IDictionaryItem> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var item = FindItem(node);

            var info = node.Element(uSyncConstants.Xml.Info);
            var alias = node.GetAlias();

            var details = new List<uSyncChange>();

            Guid? parentKey = null;
            var parentItemKey = info.Element(uSyncConstants.Xml.Parent).ValueOrDefault(string.Empty);
            if (parentItemKey != string.Empty)
            {
                var parent = _localizationService.GetDictionaryItemByKey(parentItemKey);
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
            {
                details.AddUpdate("ItemKey", item.ItemKey, alias);
                item.ItemKey = alias;
            }

            if (item.Key != key)
            {
                // If the key is different we can't update it (DB Contraints in Umbraco)
                // so we just carry on, we no longer check the key when comparing
                // so if the keys mismatch then things will continue to work.
                // renaming of mismatched key values might result in duplicates.
                // but the workarounds could also end up creating far to many extra records
                // (we would delete/recreate one each time you synced)

                logger.LogInformation("Dictionary keys (Guids) do not match - we can continue with this, but renaming dictionary items from a source computer with a mismatched key value might result in duplicate entries in your dictionary values.");
                details.AddUpdate(uSyncConstants.Xml.Key, item.Key, key);

                if (options.GetSetting<bool>("ForceKeySync", false))
                {
                    logger.LogDebug("Forcing key sync of dictionary item - if the keys are out of sync on existing items this can cause a SQL Contraint error");
                    item.Key = key;
                }
            }

            // key only translation, would not add the translation values. 
            if (!options.GetSetting("KeysOnly", false))
            {
                details.AddRange(DeserializeTranslations(item, node, options));
            }

            // this.SaveItem(item);


            return SyncAttempt<IDictionaryItem>.Succeed(item.ItemKey, item, ChangeType.Import, details);
        }

        private IEnumerable<uSyncChange> DeserializeTranslations(IDictionaryItem item, XElement node, SyncSerializerOptions options)
        {
            var translationNode = node?.Element("Translations");
            if (translationNode == null) return Enumerable.Empty<uSyncChange>();

            var currentTranslations = item.Translations.ToList();

            var activeCultures = options.GetDeserializedCultures(node);

            var changes = new List<uSyncChange>();

            foreach (var translation in translationNode.Elements("Translation"))
            {
                var language = translation.Attribute("Language").ValueOrDefault(string.Empty);
                if (language == string.Empty) continue;

                // only deserialize the active cultures passed to us (blank = all) 
                if (!activeCultures.IsValid(language)) continue;


                var itemTranslation = item.Translations.FirstOrDefault(x => x.LanguageIsoCode == language);
                if (itemTranslation != null && itemTranslation.Value != translation.Value)
                {
                    changes.AddUpdate(language, itemTranslation.Value, translation.Value, $"{item.ItemKey}/{language}");
                    itemTranslation.Value = translation.Value;

                }
                else
                {
                    var lang = _localizationService.GetLanguageByIsoCode(language);
                    if (lang != null)
                    {
                        changes.AddNew(language, translation.Value, $"{item.ItemKey}/{language}");
                        currentTranslations.Add(new DictionaryTranslation(lang, translation.Value));
                    }
                }
            }

            var translations = currentTranslations.SafeDistinctBy(x => x.LanguageIsoCode).ToList();

            // if we are syncing all cultures we do a delete, but when only syncing some, we 
            // don't remove missing cultures from the list.
            if (activeCultures.Count == 0)
            {
                // if the count is wrong, we delete the item (shortly before we save it again).
                if (item.Translations.Count() > translations.Count)
                {
                    var existing = FindItem(item.Key);
                    if (existing != null)
                    {
                        DeleteItem(existing);
                        item.Id = 0; // make this a new (so it will be inserted)
                    }
                }

            }

            item.Translations = translations; //.SafeDistinctBy(x => x.Language.IsoCode);

            return changes;
        }

        protected override SyncAttempt<XElement> SerializeCore(IDictionaryItem item, SyncSerializerOptions options)
        {
            var node = InitializeBaseNode(item, item.ItemKey, GetLevel(item));

            // if we are serializing by culture, then add the culture attribute here. 
            var cultures = options.GetSetting(uSyncConstants.CultureKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(cultures))
                node.Add(new XAttribute(uSyncConstants.CultureKey, cultures));

            var info = new XElement(uSyncConstants.Xml.Info);

            if (item.ParentId.HasValue)
            {
                var parent = FindItem(item.ParentId.Value);
                if (parent != null)
                {
                    info.Add(new XElement(uSyncConstants.Xml.Parent, parent.ItemKey));
                }
            }

            var activeCultures = options.GetCultures();

            var translationsNode = new XElement("Translations");

            foreach (var translation in item.Translations
                .SafeDistinctBy(x => x.LanguageIsoCode)
                .OrderBy(x => x.LanguageIsoCode))
            {
                if (activeCultures.IsValid(translation.LanguageIsoCode))
                {
                    translationsNode.Add(new XElement("Translation", translation.Value,
                        new XAttribute("Language", translation.LanguageIsoCode)));
                }
            }

            node.Add(info);
            node.Add(translationsNode);

            return SyncAttempt<XElement>.Succeed(
                item.ItemKey, node, typeof(IDictionaryItem), ChangeType.Export);
        }

        public override IDictionaryItem FindItem(int id)
            => _localizationService.GetDictionaryItemById(id);

        public override IDictionaryItem FindItem(Guid key)
            => _localizationService.GetDictionaryItemById(key);

        public override IDictionaryItem FindItem(string alias)
            => _localizationService.GetDictionaryItemByKey(alias);

        private int GetLevel(IDictionaryItem item, int level = 0)
        {
            if (!item.ParentId.HasValue) return level;

            var parent = FindItem(item.ParentId.Value);
            if (parent != null)
                return GetLevel(parent, level + 1);

            return level;
        }

        public override void SaveItem(IDictionaryItem item)
            => _localizationService.Save(item);

        public override void DeleteItem(IDictionaryItem item)
            => _localizationService.Delete(item);

        public override string ItemAlias(IDictionaryItem item)
            => item.ItemKey;

        protected override XElement CleanseNode(XElement node)
        {
            var clone = XElement.Parse(node.ToString());
            var keyAttribute = clone.Attribute("Key");
            if (keyAttribute != null) keyAttribute.Value = Guid.Empty.ToString();
            return base.CleanseNode(clone);  
        }
    }
}
