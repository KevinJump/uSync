using System;
using System.Xml.Serialization;

namespace Jumoo.uSync.Core.Serializers.Dtos
{
    [Serializable]
    [XmlType(TypeName = "MemberGroup")]
    public class MemberGroupDto
    { 
        public KeyGeneralSectionBase General { get; set; }
        public override bool Equals(object obj)
        {
            var anotherDto = obj as MemberGroupDto;
            if (anotherDto == null) return false;
            return GetHashCode() == anotherDto.GetHashCode();
        }

        public override int GetHashCode()
        {
            return General?.GetHashCode() ?? 0;
        }
    }
}