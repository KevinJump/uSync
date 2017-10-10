using System;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Interfaces;

namespace Jumoo.uSync.Core.Serializers
{
    public abstract class SyncExtendedSerializerBase<T> : ISyncSerializer<T>
    {
        private readonly string _itemType;

        public virtual int Priority => 100;

        public abstract string SerializerType { get; }

        protected SyncExtendedSerializerBase(string itemType)
        {
            _itemType = itemType;
        }

        public abstract bool Validate(XElement node);

        public SyncAttempt<T> DeSerialize(XElement node, bool forceUpdate = false)
        {
            try
            {
                if (!Validate(node)) return SyncAttempt<T>.Fail(node.NameFromNode(), default(T), ChangeType.Fail, $"Xml for {node.NameFromNode()} is invalid.");

                if (forceUpdate || IsUpdate(node))
                    return DeserializeCore(node);
                return SyncAttempt<T>.Succeed(node.NameFromNode(), default(T), ChangeType.NoChange);
            }
            catch (Exception excp)
            {
                return SyncAttempt<T>.Fail(node.NameFromNode(), default(T), ChangeType.Fail, excp);
            }
        }

        public virtual bool IsUpdate(XElement node)
        {
            return true;
        }

        public SyncAttempt<XElement> Serialize(T item)
        {
            return SerializeCore(item);
        }

        public abstract SyncAttempt<XElement> SerializeCore(T item);

        public abstract SyncAttempt<T> DeserializeCore(XElement node);
    }
}