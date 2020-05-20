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
        /// <summary>
        ///  Serialize and Item into uSync XML format (with options)
        /// </summary>
        SyncAttempt<XElement> Serialize(TObject item, SyncSerializerOptions options);

        /// <summary>
        ///  Deserialize an item from XML into Umbraco (with options)
        /// </summary>
        SyncAttempt<TObject> Deserialize(XElement node, SyncSerializerOptions options);

        /// <summary>
        ///  Run the second pass of a deserialization (with options)
        /// </summary>
        SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SyncSerializerOptions options);

        /// <summary>
        ///  Is the XML in sync with what is inside Umbraco? (with options)
        /// </summary>
        ChangeType IsCurrent(XElement node, SyncSerializerOptions options);
    }
}
