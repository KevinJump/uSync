using System;

namespace Jumoo.uSync.Core.Serializers.Dtos
{
    [Serializable]
    public class GeneralSectionBase
    {
        public string Name { get; set; }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}