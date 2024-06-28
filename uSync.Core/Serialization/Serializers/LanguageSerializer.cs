using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("8D2381C3-A0F8-43A2-8563-6F12F9F48023", "Language Serializer",
        uSyncConstants.Serialization.Language, IsTwoPass = true)]
    public class LanguageSerializer : SyncSerializerBase<ILanguage>, ISyncSerializer<ILanguage>
    {
        private readonly ILocalizationService _localizationService;

        [Obsolete("Will remove GlobalSettings in v11")]
        public LanguageSerializer(IEntityService entityService,
            ILogger<LanguageSerializer> logger,
            ILocalizationService localizationService,
            IOptions<GlobalSettings> options)
            : this(entityService, logger, localizationService)
        { }

        [ActivatorUtilitiesConstructor]
        public LanguageSerializer(IEntityService entityService,
            ILogger<LanguageSerializer> logger,
            ILocalizationService localizationService)
            : base(entityService, logger)
        {
            this._localizationService = localizationService;
        }


        private CultureInfo GetCulture(string isoCode)
            => CultureInfo.GetCultureInfo(isoCode);

        protected override SyncAttempt<ILanguage> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var isoCode = node.Element("IsoCode").ValueOrDefault(string.Empty);
            logger.LogDebug("Derserializing {isoCode}", isoCode);

            var item = _localizationService.GetLanguageByIsoCode(isoCode);
            var culture = GetCulture(isoCode);

            var details = new List<uSyncChange>();

            if (item == null)
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

            var fallbackId = GetFallbackLanguageId(item, node);
            if (fallbackId > 0 && item.FallbackLanguageId != fallbackId)
            {
                details.AddUpdate("FallbackId", item.FallbackLanguageId, fallbackId);
                item.FallbackLanguageId = fallbackId;
            }

            // logger.Debug<ILanguage>("Saving Language");
            //localizationService.Save(item);

            return SyncAttempt<ILanguage>.Succeed(item.CultureName, item, ChangeType.Import, details);
        }

        /// <summary>
        ///  second pass we set the default language again (because you can't just set it)
        /// </summary>
        public override SyncAttempt<ILanguage> DeserializeSecondPass(ILanguage item, XElement node, SyncSerializerOptions options)
        {
            logger.LogDebug("Language Second Pass {IsoCode}", item.IsoCode);

            var details = new List<uSyncChange>();

            var isDefault = node.Element("IsDefault").ValueOrDefault(false);
            if (item.IsDefault != isDefault)
            {
                details.AddUpdate("IsDefault", item.IsDefault, isDefault);
                item.IsDefault = isDefault;
            }

            var fallbackId = GetFallbackLanguageId(item, node);
            if (fallbackId > 0 && item.FallbackLanguageId != fallbackId)
            {
                details.AddUpdate("FallbackId", item.FallbackLanguageId, fallbackId);
                item.FallbackLanguageId = fallbackId;
            }

            if (!options.Flags.HasFlag(SerializerFlags.DoNotSave) && item.IsDirty())
                _localizationService.Save(item);

            return SyncAttempt<ILanguage>.Succeed(item.CultureName, item, ChangeType.Import, details);
        }

        private int GetFallbackLanguageId(ILanguage item, XElement node)
        {
            var fallbackIso = node.Element("Fallback").ValueOrDefault(string.Empty);
            if (!string.IsNullOrWhiteSpace(fallbackIso))
            {
                if (int.TryParse(fallbackIso, NumberStyles.Integer, CultureInfo.InvariantCulture, out int fallbackId))
                {
                    // legacy, the fallback value is an int :( 
                    return fallbackId;
                }
                else
                {
                    // 8.5+ we store the iso in the fallback value, its more reliable.
                    var fallback = _localizationService.GetLanguageByIsoCode(fallbackIso);
                    if (fallback != null)
                    {
                        return fallback.Id;
                    }
                }
            }

            return 0;

        }

        protected override XElement InitializeBaseNode(ILanguage item, string alias, int level = 0)
        {
            // language guids change all the time ! we ignore them, but here we set them to the 'id' 
            // this means the file stays the same! 

            // for backwards compatibility, use the LCID unless its
            // the 'unknown' value then use the iso-code turn that into a guid
            var key = item.CultureInfo.LCID != 4096 
                ? Int2Guid(item.CultureInfo.LCID) 
                : item.IsoCode.ToGuid();

            return new XElement(ItemType, new XAttribute(uSyncConstants.Xml.Key, key.ToString().ToLower()),
                new XAttribute(uSyncConstants.Xml.Alias, alias),
                new XAttribute(uSyncConstants.Xml.Level, level));
        }

        private Guid Int2Guid(int value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        protected override SyncAttempt<XElement> SerializeCore(ILanguage item, SyncSerializerOptions options)
        {
            var node = InitializeBaseNode(item, item.IsoCode);

            // don't serialize the ID, it changes and we don't use it! 
            // node.Add(new XElement("Id", item.Id));
            node.Add(new XElement("Name", item.CultureName));
            node.Add(new XElement("IsoCode", item.IsoCode));
            node.Add(new XElement("IsMandatory", item.IsMandatory));
            node.Add(new XElement("IsDefault", item.IsDefault));

            if (item.FallbackLanguageId != null)
            {
                var fallback = _localizationService.GetLanguageById(item.FallbackLanguageId.Value);
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

        public override ILanguage FindItem(string alias)
        {
            // GetLanguageByIsoCode - doesn't only return the language of the code you specify
            // it will fallback to the primary one (e.g en-US might return en), 
            //
            // based on that we need to check that the language we get back actually has the 
            // code we asked for from the api.
            var item = _localizationService.GetLanguageByIsoCode(alias);
            if (item == null || !item.CultureInfo.Name.InvariantEquals(alias)) return null;
            return item;
        }

        public override ILanguage FindItem(int id)
            => _localizationService.GetLanguageById(id);

        public override ILanguage FindItem(Guid key) => default(ILanguage);

        public override void SaveItem(ILanguage item)
            => _localizationService.Save(item);

        public override void DeleteItem(ILanguage item)
            => _localizationService.Delete(item);


        protected override XElement CleanseNode(XElement node)
        {
            if (node?.Attribute(uSyncConstants.Xml.Key) != null)
                node.Attribute(uSyncConstants.Xml.Key).Value = "";
            return node;
        }

        public override string ItemAlias(ILanguage item)
            => item.IsoCode;

    }
}
