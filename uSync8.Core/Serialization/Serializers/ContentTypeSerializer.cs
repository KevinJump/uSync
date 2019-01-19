using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Serialization.Serializers
{
    [USyncSerializer("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer")]
    public class ContentTypeSerializer : USyncSerializerBase<IContentType>
    {
        public Type UmbracoObjectType => typeof(IContentType);

        public override SyncAttempt<XElement> Serialize(IContentType item)
        {
            throw new NotImplementedException();
        }

        public override SyncAttempt<IContentType> Deserialize(XElement node, bool force)
        {
            throw new NotImplementedException();
        }

        public override bool IsCurrent(XElement node)
        {
            throw new NotImplementedException();
        }
    }
}
