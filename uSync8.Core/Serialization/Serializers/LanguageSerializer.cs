using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("8D2381C3-A0F8-43A2-8563-6F12F9F48023", "Language Serializer", uSyncConstants.Serialization.Language)]
    public class LanguageSerializer : SyncSerializerBase<ILanguage>, ISyncSerializer<ILanguage>
    {
        private readonly ILocalizationService localizationService;

        public LanguageSerializer(IEntityService entityService,
            ILocalizationService localizationService)
            : base(entityService)
        {
            this.localizationService = localizationService;
        }

        protected override SyncAttempt<ILanguage> DeserializeCore(XElement node)
        {
            if (node.Element("CultureName") == null 
                || node.Element("IsoCode") == null)
            {
                throw new ArgumentException("Invalid XML");
            }

            var isoCode = node.Element("IsoCode").ValueOrDefault(string.Empty);

            var item = localizationService.GetLanguageByIsoCode(isoCode);

            if (item == null)
                item = new Language(isoCode);

            item.IsoCode = isoCode;
            item.CultureName = node.Element("CultureName").ValueOrDefault(string.Empty);
            item.IsDefault = node.Element("IsDefault").ValueOrDefault(false);
            item.IsMandatory = node.Element("IsMandatory").ValueOrDefault(false);

            var fallback = node.Element("Fallback").ValueOrDefault(0);
            if (fallback > 0)
                item.FallbackLanguageId = fallback;

            localizationService.Save(item);

            return SyncAttempt<ILanguage>.Succeed(item.CultureName, item, ChangeType.Import);
        }

        protected override SyncAttempt<XElement> SerializeCore(ILanguage item)
        {
            var node = new XElement(ItemType,
                new XAttribute("Key", item.Key),
                new XElement("Id", item.Id),
                new XElement("IsoCode", item.IsoCode),
                new XElement("CultureName", item.CultureName),
                new XElement("IsMandatory", item.IsMandatory),
                new XElement("IsDefault", item.IsDefault));

            if (item.FallbackLanguageId != null)
                node.Add(new XElement("Fallback", item.FallbackLanguageId.Value));

            return SyncAttempt<XElement>.SucceedIf(
                node != null,
                item.CultureName,
                node,
                typeof(ILanguage),
                ChangeType.Export);
        }
    }
}
