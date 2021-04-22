using System;

namespace uSync.Core.Serialization
{
    public sealed class SyncSerializerAttribute : Attribute
    {
        public SyncSerializerAttribute(string id, string name, string itemType)
        {
            Id = new Guid(id);
            Name = name;
            ItemType = itemType;
        }

        public string Name { get; private set; }
        public Guid Id { get; private set; }
        public string ItemType { get; private set; }

        public int Priority { get; set; }

        public bool IsTwoPass { get; set; }
    }
}
