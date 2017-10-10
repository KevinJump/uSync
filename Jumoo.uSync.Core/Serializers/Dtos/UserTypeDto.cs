using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Jumoo.uSync.Core.Serializers.Dtos
{
    [Serializable]
    [XmlType(TypeName = "UserType")]
    public class UserTypeDto
    {
        public UserTypeGeneralSection General { get; set; }

        [XmlArrayItem("Permission")]
        public List<string> Permissions { get; set; }

        public override bool Equals(object obj)
        {
            var anotherDto = obj as UserTypeDto;
            if (anotherDto == null) return false;
            return GetHashCode() == anotherDto.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = General?.GetHashCode() ?? 0;
            Permissions?.ForEach(s =>
            {
                hash = (hash * 7) + s.GetHashCode();
            });
            return hash;
        }
    }

    [Serializable]
    public class UserTypeGeneralSection : GeneralSectionBase
    {
        public string Alias { get; set; }
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = (hash * 7) + Alias.GetHashCode();
            return hash;
        }
    }
}