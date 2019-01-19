using System;

namespace uSync8.Core.Serialization
{
    public sealed class USyncSerializerAttribute : Attribute
    {
        public USyncSerializerAttribute(string id, string name)
        {
            Id = new Guid(id);
            Name = name;
        }

        public string Name { get; private set; }
        public Guid Id { get; private set; }

        public int Priority { get; set; }
    }
}
