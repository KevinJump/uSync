using System;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

namespace uSync8.Core.Serialization
{
    public abstract class SyncSerializerBase<TObject> : IDiscoverable
        where TObject : IEntity
    {
        protected readonly IEntityService entityService;

        protected SyncSerializerBase(IEntityService entityService)
        {
            // read the attribute
            var thisType = GetType();
            var meta = thisType.GetCustomAttribute<SyncSerializerAttribute>(false);
            if (meta == null)
                throw new InvalidOperationException($"the uSyncSerializer {thisType} requires a {typeof(SyncSerializerAttribute)}");

            Name = meta.Name;
            Id = meta.Id;
            ItemType = meta.ItemType;

            IsTwoPass = meta.IsTwoPass;

            // base services 
            this.entityService = entityService;

        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public string ItemType { get; set; }

        public Type UmbracoObjectType => typeof(TObject);

        public bool IsTwoPass { get; private set; }


        public SyncAttempt<XElement> Serialize(TObject item)
        {
            return SerializeCore(item);
        }

        public SyncAttempt<TObject> Deserialize(XElement node, bool force, bool OnePass)
        {
            if (node.Name.LocalName != this.ItemType)
                throw new ArgumentException($"XML Not valid for type {ItemType}");

            if (force || !IsCurrent(node))
            {
                var result = DeserializeCore(node);
                if (OnePass && result.Success)
                {
                    DesrtializeSecondPass(result.Item, node);
                }

                return result;
            }



            return SyncAttempt<TObject>.Succeed(node.Name.LocalName, default(TObject), ChangeType.NoChange);
        }

        public virtual SyncAttempt<TObject> DesrtializeSecondPass(TObject item, XElement node)
        {
            return SyncAttempt<TObject>.Succeed(nameof(item), item, typeof(TObject), ChangeType.NoChange);

        }

        protected abstract SyncAttempt<XElement> SerializeCore(TObject item);
        protected abstract SyncAttempt<TObject> DeserializeCore(XElement node);

        public virtual bool IsCurrent(XElement node)
        {
            return false;
        }

        protected virtual XElement InitializeBaseNode(TObject item)
        {
            return new XElement(ItemType,
                new XAttribute("Key", item.Key));
        }
    }
}
