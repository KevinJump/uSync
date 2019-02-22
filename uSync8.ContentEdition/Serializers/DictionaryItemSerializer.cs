using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    [SyncSerializer("4D18F4C3-6EBC-4AAD-8D20-6353BDBBD484", "Dicrionary Serializer", uSyncConstants.Serialization.Dicrionary)]
    public class DictionaryItemSerializer : SyncSerializerBase<IDictionaryItem>, ISyncSerializer<IDictionaryItem>
    {
        private readonly ILocalizationService localizationService;

        public DictionaryItemSerializer(IEntityService entityService, ILogger logger,
            ILocalizationService localizationService) 
            : base(entityService, logger)
        {
            this.localizationService = localizationService;
        }

        protected override SyncAttempt<IDictionaryItem> DeserializeCore(XElement node)
        {
            return SyncAttempt<IDictionaryItem>.Fail(node.GetAlias(), ChangeType.Fail, new NotImplementedException());
        }

        protected override SyncAttempt<XElement> SerializeCore(IDictionaryItem item)
        {
            var node = InitializeBaseNode(item, item.ItemKey, GetLevel(item));

            var info = new XElement("Info");

            if (item.ParentId.HasValue)
            {
                var parent = FindItem(item.ParentId.Value);
                if (parent != null)
                {
                    info.Add(new XElement("Parent", parent.ItemKey,
                        new XAttribute("Key", item.ParentId)));
                }
            }

            var translationsNode = new XElement("Translations");

            foreach(var translation in item.Translations)
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

    }
}
