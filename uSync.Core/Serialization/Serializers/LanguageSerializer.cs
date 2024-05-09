using System.Globalization;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

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
		ILanguageService languageService)
		: base(entityService, logger)
	{
		_languageService = languageService;
	}


	private static CultureInfo GetCulture(string isoCode) => CultureInfo.GetCultureInfo(isoCode);

	protected override async Task<SyncAttempt<ILanguage>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{
        var isoCode = node.Element("IsoCode").ValueOrDefault(string.Empty);
        logger.LogDebug("Deserializing {isoCode}", isoCode);

		// TODO: we might want to cache this, as it means we are going to call it every time we import a language
		var item = await FindByIsoCodeAsync(isoCode);
        var culture = GetCulture(isoCode);

        var details = new List<uSyncChange>();

        if (item is null)
        {
            logger.LogDebug("Creating New Language: {isoCode}", isoCode);
            item = new Language(isoCode, culture.DisplayName);
            details.AddNew(isoCode, isoCode, "Language");
        }

        if (item.IsoCode != isoCode)
        {
            details.AddUpdate("IsoCode", item.IsoCode, isoCode);
            item.IsoCode = isoCode;
        }

        var name = node.Element("Name").ValueOrDefault(string.Empty);
        if (string.IsNullOrEmpty(name))
        {
            try
            {
                if (item.CultureName != culture.DisplayName)
                {
                    details.AddUpdate("CultureName", item.CultureName, culture.DisplayName);
                    item.CultureName = culture.DisplayName;
                }
            }
            catch
            {
                logger.LogWarning("Can't set culture name based on IsoCode");
            }
        }
        else if (item.CultureName != name)
        {
            details.AddUpdate("CultureName", item.CultureName, name);
            item.CultureName = name;
        }

        var mandatory = node.Element("IsMandatory").ValueOrDefault(false);
        if (item.IsMandatory != mandatory)
        {
            details.AddUpdate("IsMandatory", item.IsMandatory, mandatory);
            item.IsMandatory = mandatory;
        }

        var isDefault = node.Element("IsDefault").ValueOrDefault(false);
        if (item.IsDefault != isDefault)
        {
            details.AddUpdate("IsDefault", item.IsDefault, isDefault);
            item.IsDefault = isDefault;
        }

        var fallbackIsoCode = GetFallbackLanguageIsoCode(item, node);
        if (!string.IsNullOrEmpty(fallbackIsoCode) && item.FallbackIsoCode != fallbackIsoCode)
        {
            details.AddUpdate("FallbackIsoCode", item.FallbackIsoCode ?? "(None)", fallbackIsoCode);
            item.FallbackIsoCode = fallbackIsoCode;
        }

        // logger.Debug<ILanguage>("Saving Language");
        //localizationService.Save(item);

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
        if (item.IsDefault != isDefault)
        {
            details.AddUpdate("IsDefault", item.IsDefault, isDefault);
            item.IsDefault = isDefault;
        }

        var fallbackIsoCode = GetFallbackLanguageIsoCode(item, node);
        if (!string.IsNullOrWhiteSpace(fallbackIsoCode) && item.FallbackIsoCode != fallbackIsoCode)
        {
            details.AddUpdate("FallbackIsoCode", item.FallbackIsoCode ?? "(None)", fallbackIsoCode);
            item.FallbackIsoCode = fallbackIsoCode;
        }

        if (!options.Flags.HasFlag(SerializerFlags.DoNotSave) && item.IsDirty())
            await SaveItemAsync(item, options.UserKey);

		return SyncAttempt<ILanguage>.Succeed(item.CultureName, item, ChangeType.Import, details);
    }

    private static string GetFallbackLanguageIsoCode(ILanguage item, XElement node)
        => node.Element("Fallback").ValueOrDefault(string.Empty);

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
            var fallback = await FindByIsoCodeAsync(item.FallbackIsoCode);
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
        var item = await FindByIsoCodeAsync(alias);
        if (item == null || item.CultureInfo?.Name.InvariantEquals(alias) is false) return null;
        return item;
    }

    private async Task<ILanguage?> FindByIsoCodeAsync(string code)
		=> await _languageService.GetAsync(code);

	public override async Task SaveAsync(IEnumerable<ILanguage?> items, Guid userKey)
	{
        foreach(var item in items)
        {
            await SaveItemAsync(item, userKey);
        }

	}

    protected override XElement CleanseNode(XElement node)
    {
        // v14 languages have keys now, and we want to use them if we can.

        //if (node?.Attribute(uSyncConstants.Xml.Key)?.Value is not null)
        //    node.Attribute(uSyncConstants.Xml.Key)!.Value = "";

        return node!;
    }

    public override string ItemAlias(ILanguage item)
        => item.IsoCode;

	public override Task<ILanguage?> FindItemAsync(Guid key)
        => _languageService.GetAllAsync().ContinueWith(x => x.Result.FirstOrDefault(l => l.Key == key));

	public override async Task SaveItemAsync(ILanguage? item, Guid userKey)
	{
        if (item is null) return;

        if (item.Id <= 0)
			await _languageService.CreateAsync(item, userKey);
		else
			await _languageService.UpdateAsync(item, userKey);
	}

    public override async Task DeleteItemAsync(ILanguage? item, Guid userKey)
    {
        if (item is null) return;
		await _languageService.DeleteAsync(item.IsoCode, userKey);
    }
}
