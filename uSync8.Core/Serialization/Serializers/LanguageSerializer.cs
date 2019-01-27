using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

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
            var node = InitializeBaseNode(item, item.IsoCode);

            node.Add(new XElement("Id", item.Id));
            node.Add(new XElement("IsoCode", item.IsoCode));
            node.Add(new XElement("CultureName", item.CultureName));
            node.Add(new XElement("IsMandatory", item.IsMandatory));
            node.Add(new XElement("IsDefault", item.IsDefault));

            if (item.FallbackLanguageId != null)
                node.Add(new XElement("Fallback", item.FallbackLanguageId.Value));

            return SyncAttempt<XElement>.SucceedIf(
                node != null,
                item.CultureName,
                node,
                typeof(ILanguage),
                ChangeType.Export);
        }

        public override bool IsValid(XElement node)
            => (base.IsValid(node) && node.Element("CultureName") != null && node.Element("IsoCode") != null);

        protected override ILanguage GetItem(string alias) =>
            localizationService.GetLanguageByIsoCode(alias);

        protected override ILanguage GetItem(Guid key) => default(ILanguage);

        protected override XElement CleanseNode(XElement node)
        {
            node.Attribute("Key").Value = "";
            return node;
        }

    }
}
