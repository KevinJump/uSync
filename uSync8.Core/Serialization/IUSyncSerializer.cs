using System;
using System.Xml.Linq;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Serialization
{
    /// <summary>
    ///  Serializer interface (this should be generic? - but then how do i load them into the composition?)
    /// </summary>
    public interface IUSyncSerializer
    {
        SyncAttempt<XElement> Serialize(IUmbracoEntity item);
        SyncAttempt<IUmbracoEntity> Deserialize(XElement node, bool force);

        bool IsCurrent(XElement node);
        Type UmbracoObjectType { get; }
    }

    /// <summary>
    ///  Generic Serializer ideally we want to load this one into the composition, but 
    ///  it's type is generic?
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    public interface ISyncSerializer<TObject>
        where TObject : IEntity
    {
        SyncAttempt<XElement> Serialize(TObject item);
        SyncAttempt<IUmbracoEntity> Deserialize(XElement node, bool force);

        bool IsCurrent(XElement node);
    }
}
