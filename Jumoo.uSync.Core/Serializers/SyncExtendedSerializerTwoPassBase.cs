using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;

namespace Jumoo.uSync.Core.Serializers
{
    public abstract class SyncExtendedSerializerTwoPassBase<T> : SyncExtendedSerializerBase<T>, ISyncExtendedSerializer<T>
    {
        protected SyncExtendedSerializerTwoPassBase(string itemType) : base(itemType) { }

        public SyncAttempt<T> DesearlizeSecondPass(T item, XElement node)
        {
            try
            {
                return DeSearlizeSecondPass(item, node);
            }
            catch (Exception excp)
            {
                return SyncAttempt<T>.Fail(node.NameFromNode(), default(T), ChangeType.Fail, excp);
            }
        }

        public SyncAttempt<T> Deserialize(XElement node, bool forceUpdate, bool onePass)
        {
            var syncAttempt = DeSerialize(node, false);
            if (!onePass || !syncAttempt.Success || syncAttempt.Item == null)
                return syncAttempt;
            return DesearlizeSecondPass(syncAttempt.Item, node);
        }

        public abstract SyncAttempt<T> DeSearlizeSecondPass(T item, XElement node);
        public abstract IEnumerable<uSyncChange> GetChanges(XElement node);
    }
}