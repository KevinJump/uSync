using System;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Serialization
{
    public abstract class USyncSerializerBase<TObject> : IDiscoverable
        where TObject : IEntity
    {
        protected USyncSerializerBase()
        {
            var thisType = GetType();
            var meta = thisType.GetCustomAttribute<USyncSerializerAttribute>(false);
            if (meta == null)
                throw new InvalidOperationException($"the uSyncSerializer {thisType} requires a {typeof(USyncSerializerAttribute)}");

            Name = meta.Name;
            Id = meta.Id;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public abstract SyncAttempt<XElement> Serialize(TObject item);
        public abstract SyncAttempt<TObject> Deserialize(XElement node, bool force);
        public abstract bool IsCurrent(XElement node);
    }
}
