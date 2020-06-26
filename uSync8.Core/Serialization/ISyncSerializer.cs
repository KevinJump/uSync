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
    {
        /// <summary>
        ///  Find an Item based on the XML node representation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        TObject FindItem(XElement node);

        /// <summary>
        ///  Serialize an item into uSync xml format
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [Obsolete("Use with SyncSerializerOptions")]
        SyncAttempt<XElement> Serialize(TObject item);

        /// <summary>
        ///  Serialize an empty xml marker, with proposed action
        /// </summary>
        SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias);

        /// <summary>
        ///  Deserialize an usync xml representation of a item into that item.
        /// </summary>
        /// <param name="node">XML representation</param>
        /// <param name="flags">Modifier flags for serialization</param>
        [Obsolete("Use with SyncSerializerOptions")]
        SyncAttempt<TObject> Deserialize(XElement node, SerializerFlags flags);

        /// <summary>
        ///  perform a second pass of the serialization
        /// </summary>
        /// <remarks>
        ///  some things can't be serialized the first time, or require another pass once everything
        ///  else is serialized (e.g datatypes that have document type refrences in them, need the doctypes
        ///  to be created).
        /// </remarks>
        [Obsolete("Use with SyncSerializerOptions")]
        SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SerializerFlags flags);

        /// <summary>
        ///  Returns true if the peice of xml is valid for this serializer
        /// </summary>
        bool IsValid(XElement node);

        /// <summary>
        ///  tells us if this is an empty node (so result of a rename or delete)
        /// </summary>
        bool IsEmpty(XElement node);

        /// <summary>
        ///  Does the xml match teh item in umbraco, or are their changes to be made?
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>

        [Obsolete("Use with SyncSerializerOptions")]
        ChangeType IsCurrent(XElement node);

        /// <summary>
        ///  this serializer has two passes
        /// </summary>
        bool IsTwoPass { get; }

        /// <summary>
        ///  string representation of the item types used in this serializer
        /// </summary>
        string ItemType { get; }
        
        /// <summary>
        ///  save all the items in umbraco.
        /// </summary>
        /// <param name="items"></param>

        void Save(IEnumerable<TObject> items);
    }
}
