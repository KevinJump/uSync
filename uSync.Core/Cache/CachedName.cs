
using System;

namespace uSync.Core.Cache
{
    public class CachedName
    {
        public CachedName() { }
        public CachedName(Guid key, string name)
        {
            Key = key;
            Name = name;
        }

        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
