using System.Xml.Linq;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("4D18F4C3-6EBC-4AAD-8D20-6353BDBBD484", "Dictionary Serializer", uSyncConstants.Serialization.Dictionary)]
public class DictionaryItemSerializer : SyncSerializerBase<IDictionaryItem>, ISyncSerializer<IDictionaryItem>
{
    private readonly ILanguageService _languageService;
    private readonly IDictionaryItemService _dictionaryItemService;

    public DictionaryItemSerializer(IEntityService entityService, ILogger<DictionaryItemSerializer> logger,
        IDictionaryItemService localizationService, ILanguageService languageService)
        : base(entityService, logger)
    {
        this._dictionaryItemService = localizationService;
        _languageService = languageService;
    }

    protected override async Task<SyncAttempt<IDictionaryItem>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var item = await FindItemAsync(node);

        var info = node.Element(uSyncConstants.Xml.Info);
        var alias = node.GetAlias();

        var details = new List<uSyncChange>();

        Guid? parentKey = null;
        var parentItemKey = info?.Element(uSyncConstants.Xml.Parent).ValueOrDefault(string.Empty) ?? string.Empty;
        if (parentItemKey != string.Empty)
        {
            var parent = await _dictionaryItemService.GetAsync(parentItemKey);
            if (parent != null)
            {
                parentKey = parent.Key;
            }
        }

        var key = node.GetKey();

        if (item == null)
        {
            item = new DictionaryItem(parentKey, alias)
            {
                Key = key
            };
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
            // If the key is different we can't update it (DB Constraints in Umbraco)
            // so we just carry on, we no longer check the key when comparing
            // so if the keys mismatch then things will continue to work.
            // renaming of mismatched key values might result in duplicates.
            // but the workarounds could also end up creating far to many extra records
            // (we would delete/recreate one each time you synced)

            logger.LogInformation("Dictionary keys (Guids) do not match - we can continue with this, but renaming dictionary items from a source computer with a mismatched key value might result in duplicate entries in your dictionary values.");
            details.AddUpdate(uSyncConstants.Xml.Key, item.Key, key);

            if (options.GetSetting<bool>("ForceKeySync", false))
            {
                logger.LogDebug("Forcing key sync of dictionary item - if the keys are out of sync on existing items this can cause a SQL Constraint error");
                item.Key = key;
            }
        }

        // key only translation, would not add the translation values. 
        if (!options.GetSetting("KeysOnly", false))
        {
            details.AddRange(await DeserializeTranslationsAsync(item, node, options));
        }

        // this.SaveItem(item);


        return SyncAttempt<IDictionaryItem>.Succeed(item.ItemKey, item, ChangeType.Import, details);
    }

    private async Task<List<uSyncChange>> DeserializeTranslationsAsync(IDictionaryItem item, XElement node, SyncSerializerOptions options)
    {
        var translationNode = node.Element("Translations");
        if (translationNode == null) return [];

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
                var lang = await _languageService.GetAsync(language);
                if (lang != null)
                {
                    changes.AddNew(language, translation.Value, $"{item.ItemKey}/{language}");
                    currentTranslations.Add(new DictionaryTranslation(lang, translation.Value));
                }
            }
        }

        var translations = currentTranslations.DistinctBy(x => x.LanguageIsoCode).ToList();

        // if we are syncing all cultures we do a delete, but when only syncing some, we 
        // don't remove missing cultures from the list.
        if (activeCultures.Count == 0)
        {
            // if the count is wrong, we delete the item (shortly before we save it again).
            if (item.Translations.Count() > translations.Count)
            {
                var existing = await FindItemAsync(item.Key);
                if (existing != null)
                {
                    await DeleteItemAsync(existing);
                    item.Id = 0; // make this a new (so it will be inserted)
                }
            }

        }

        item.Translations = translations; //.SafeDistinctBy(x => x.Language.IsoCode);

        return changes;
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IDictionaryItem item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, item.ItemKey, await GetLevelAsync(item));

        // if we are serializing by culture, then add the culture attribute here. 
        var cultures = options.GetSetting(uSyncConstants.CultureKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(cultures))
            node.Add(new XAttribute(uSyncConstants.CultureKey, cultures));

        var info = new XElement(uSyncConstants.Xml.Info);

        if (item.ParentId.HasValue)
        {
            var parent = await FindItemAsync(item.ParentId.Value);
            if (parent != null)
            {
                info.Add(new XElement(uSyncConstants.Xml.Parent, parent.ItemKey));
            }
        }

        var activeCultures = options.GetCultures();

        var translationsNode = new XElement("Translations");

        foreach (var translation in item.Translations
            .DistinctBy(x => x.LanguageIsoCode)
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

    public override Task<IDictionaryItem?> FindItemAsync(Guid key)
        => _dictionaryItemService.GetAsync(key);

    public override Task<IDictionaryItem?> FindItemAsync(string alias)
        => _dictionaryItemService.GetAsync(alias);

    private async Task<int> GetLevelAsync(IDictionaryItem item, int level = 0)
    {
        if (!item.ParentId.HasValue) return level;

        var parent = await FindItemAsync(item.ParentId.Value);
        if (parent is not null)
            return await GetLevelAsync(parent, level + 1);

        return level;
    }

    public override async Task SaveItemAsync(IDictionaryItem item)
        => _ = item.HasIdentity 
            ? await _dictionaryItemService.UpdateAsync(item, Constants.Security.SuperUserKey) 
            : await _dictionaryItemService.CreateAsync(item, Constants.Security.SuperUserKey);

    public override Task DeleteItemAsync(IDictionaryItem item)
        => _dictionaryItemService.DeleteAsync(item.Key, Constants.Security.SuperUserKey);

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
