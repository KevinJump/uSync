using System;

namespace Jumoo.uSync.Core.Serializers.Dtos
{
    [Serializable]
    public class KeyGeneralSectionBase : GeneralSectionBase
    {
        public Guid Key { get; set; }
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = (hash * 7) + Key.GetHashCode();
            return hash;
        }
    }
}