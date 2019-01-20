using System;
using System.Xml.Linq;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Serialization
{
   
    public interface ISyncSerializerBase
    {
        Type UmbracoObjectType { get; }
    }

    /// <summary>
    ///  Generic Serializer ideally we want to load this one into the composition, but 
    ///  it's type is generic?
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    public interface ISyncSerializer<TObject> : ISyncSerializerBase
        where TObject : IEntity
    {
        SyncAttempt<XElement> Serialize(TObject item);
        SyncAttempt<TObject> Deserialize(XElement node, bool force);

        bool IsCurrent(XElement node);
    }
}
