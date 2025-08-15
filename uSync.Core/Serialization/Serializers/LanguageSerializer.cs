using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("8D2381C3-A0F8-43A2-8563-6F12F9F48023", "Language Serializer",
    uSyncConstants.Serialization.Language, IsTwoPass = true)]
public class LanguageSerializer : SyncSerializerBase<ILanguage>, ISyncSerializer<ILanguage>
{
    private readonly ILanguageService _languageService;

    public LanguageSerializer(IEntityService entityService,
        ILogger<LanguageSerializer> logger,
        ILanguageService localizationService)
        : base(entityService, logger)
    {
        _languageService = localizationService;
    }

    protected override async Task<SyncAttempt<ILanguage>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();

        var isoCode = node.Element("IsoCode").ValueOrDefault(string.Empty);
        var culture = GetCulture(isoCode);

        var item = await _languageService.GetAsync(isoCode)
            ?? new Language(isoCode, culture.DisplayName);

        if (item.HasIdentity is false)
            details.AddNew(isoCode, isoCode, "Language");

        details.AddIfUpdated(nameof(item.IsoCode), item.IsoCode, isoCode);
        item.IsoCode = isoCode;

        var cultureName = node.Element("Name").ValueOrDefault(string.Empty);
        if (string.IsNullOrEmpty(cultureName)) cultureName = culture.DisplayName;
        details.AddIfUpdated(nameof(item.CultureName), item.CultureName, cultureName);
        item.CultureName = cultureName;

        var mandatory = node.Element("IsMandatory").ValueOrDefault(false);
        details.AddIfUpdated(nameof(item.IsMandatory), item.IsMandatory, mandatory);
        item.IsMandatory = mandatory;

        var isDefault = node.Element("IsDefault").ValueOrDefault(false);
        details.AddIfUpdated(nameof(item.IsDefault), item.IsDefault, isDefault);
        item.IsDefault = isDefault;

        var fallbackIsoCode = GetFallbackLanguageIsoCode(item, node);
        details.AddIfUpdated(nameof(item.FallbackIsoCode), item.FallbackIsoCode, fallbackIsoCode);
        item.FallbackIsoCode = fallbackIsoCode;

        return SyncAttempt<ILanguage>.Succeed(item.CultureName, item, ChangeType.Import, details);
    }

    /// <summary>
    ///  second pass we set the default language again (because you can't just set it)
    /// </summary>
    public override async Task<SyncAttempt<ILanguage>> DeserializeSecondPassAsync(ILanguage item, XElement node, SyncSerializerOptions options)
    {
        logger.LogDebug("Language Second Pass {IsoCode}", item.IsoCode);

        var details = new List<uSyncChange>();

        var isDefault = node.Element("IsDefault").ValueOrDefault(false);
        details.AddIfUpdated(nameof(item.IsDefault), item.IsDefault, isDefault);
        item.IsDefault = isDefault;

        var fallbackIsoCode = GetFallbackLanguageIsoCode(item, node);
        details.AddIfUpdated(nameof(item.FallbackIsoCode), item.FallbackIsoCode, fallbackIsoCode);
        item.FallbackIsoCode = fallbackIsoCode;

        if (!options.Flags.HasFlag(SerializerFlags.DoNotSave) && item.IsDirty())
            await SaveItemAsync(item);

        return SyncAttempt<ILanguage>.Succeed(item.CultureName, item, ChangeType.Import, details);
    }

    private static string? GetFallbackLanguageIsoCode(ILanguage item, XElement node)
        => node.Element("Fallback").ValueOrDefault<string?>(null);

    protected override XElement InitializeBaseNode(ILanguage item, string alias, int level = 0)
    {
        // language guids change all the time ! we ignore them, but here we set them to the 'id' 
        // this means the file stays the same! 
        var key = item.CultureInfo?.Name.GetDeterministicHashCode().ConvertToGuid() ?? item.Key;

        return new XElement(ItemType, new XAttribute(uSyncConstants.Xml.Key, key.ToString().ToLower()),
            new XAttribute(uSyncConstants.Xml.Alias, alias),
            new XAttribute(uSyncConstants.Xml.Level, level));
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(ILanguage item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, item.IsoCode);

        // don't serialize the ID, it changes and we don't use it! 
        // node.Add(new XElement("Id", item.Id));
        node.Add(new XElement("Name", item.CultureName));
        node.Add(new XElement("IsoCode", item.IsoCode));
        node.Add(new XElement("IsMandatory", item.IsMandatory));
        node.Add(new XElement("IsDefault", item.IsDefault));

        if (string.IsNullOrEmpty(item.FallbackIsoCode) is false)
        {
            var fallback = await _languageService.GetAsync(item.FallbackIsoCode);
            if (fallback != null)
            {
                node.Add(new XElement("Fallback", fallback.IsoCode));
            }
        }

        return SyncAttempt<XElement>.SucceedIf(
            node != null,
            item.CultureName,
            node,
            typeof(ILanguage),
            ChangeType.Export);
    }

    public override bool IsValid(XElement node)
        => node.Name.LocalName == this.ItemType
            && node.GetAlias() != string.Empty
            && node.Element("IsoCode") != null;

    public override async Task<ILanguage?> FindItemAsync(string alias)
    {
        // GetLanguageByIsoCode - doesn't only return the language of the code you specify
        // it will fallback to the primary one (e.g en-US might return en), 
        //
        // based on that we need to check that the language we get back actually has the 
        // code we asked for from the api.
        var item = await _languageService.GetAsync(alias);
        if (item == null || item.CultureInfo?.Name.InvariantEquals(alias) is false) return null;
        return item;
    }

    private static CultureInfo GetCulture(string isoCode) => CultureInfo.GetCultureInfo(isoCode);

    /// <summary>
    ///  Keys for languages are not stable, the IsoCode is the best way to find a language. 
    /// </summary>
    public override Task<ILanguage?> FindItemAsync(Guid key)
        => Task.FromResult(default(ILanguage));

    public override async Task SaveItemAsync(ILanguage item)
        => _ = item.HasIdentity
            ? await _languageService.UpdateAsync(item, Constants.Security.SuperUserKey)
            : await _languageService.CreateAsync(item, Constants.Security.SuperUserKey);

    public override Task DeleteItemAsync(ILanguage item)
        => _languageService.DeleteAsync(item.IsoCode, Constants.Security.SuperUserKey);

    public override string ItemAlias(ILanguage item)
        => item.IsoCode;
}
