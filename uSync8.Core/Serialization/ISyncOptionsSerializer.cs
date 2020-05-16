using System.Xml.Linq;

using Umbraco.Core.Models.Entities;

using uSync8.Core.Models;

namespace uSync8.Core.Serialization
{
    /// <summary>
    ///  Serializer that can take options to the main methods.
    /// </summary>
    public interface ISyncOptionsSerializer<TObject> : ISyncSerializer<TObject>
        where TObject : IEntity
    {
        SyncAttempt<XElement> Serialize(TObject item, SyncSerializerOptions options);

        SyncAttempt<TObject> Deserialize(XElement node, SyncSerializerOptions options);

        SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SyncSerializerOptions options);

        ChangeType IsCurrent(XElement node, SyncSerializerOptions options);
    }
}
