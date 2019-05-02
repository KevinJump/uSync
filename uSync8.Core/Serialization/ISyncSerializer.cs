using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Umbraco.Core.Models.Entities;

using uSync8.Core.Models;

namespace uSync8.Core.Serialization
{

    public interface ISyncSerializerBase
    {
        Type objectType { get; }
    }

    /// <summary>
    ///  Generic Serializer ideally we want to load this one into the composition, but 
    ///  it's type is generic?
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    public interface ISyncSerializer<TObject> : ISyncSerializerBase
        where TObject : IEntity
    {
        TObject FindItem(XElement node);

        SyncAttempt<XElement> Serialize(TObject item);

        SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias);

        SyncAttempt<TObject> Deserialize(XElement node, SerializerFlags flags);
        SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SerializerFlags flags);

        /// <summary>
        ///  Returns true if the peice of xml is valid for this serializer
        /// </summary>
        bool IsValid(XElement node);

        /// <summary>
        ///  tells us if this is an empty node (so result of a rename or delete)
        /// </summary>
        bool IsEmpty(XElement node);

        ChangeType IsCurrent(XElement node);

        bool IsTwoPass { get; }

        string ItemType { get; }

        void Save(IEnumerable<TObject> items);
    }
}
