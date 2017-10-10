using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Jumoo.uSync.Core.Serializers.Dtos
{
    [Serializable]
    [XmlType(TypeName = "Member")]
    public class MemberDto
    { 
        public MemberGeneralSection General { get; set; }
        [XmlArrayItem("Property")]
        public List<Property> Properties { get; set; }
        [XmlArrayItem("Role")]
        public List<String> Roles { get; set; }

        public override bool Equals(object obj)
        {
            var anotherDto = obj as MemberDto;
            if (anotherDto == null) return false;
            return GetHashCode() == anotherDto.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = General?.GetHashCode() ?? 0;
            Properties?.ForEach(s =>
            {
                hash = (hash * 7) + s.GetHashCode();
            });
            Roles?.ForEach(s =>
            {
                hash = (hash * 7) + s.GetHashCode();
            });
            return hash;
        }
    }

    [Serializable]
    public class MemberGeneralSection : KeyGeneralSectionBase
    {
        public string ContentTypeAlias { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string RawPassword { get; set; }
        public string RawPasswordAnswer { get; set; }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = (hash * 7) + Email?.GetHashCode() ?? 0;
            hash = (hash * 7) + Username?.GetHashCode() ?? 0;
            hash = (hash * 7) + ContentTypeAlias?.GetHashCode() ?? 0;
            return hash;
        }
    }

    [Serializable]
    public class Property
    {
        [XmlAttribute]
        public string Alias { get; set; }
        [XmlText]
        public string Value { get; set; }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Alias?.GetHashCode() ?? 0;
            hash = (hash * 7) + Value?.GetHashCode() ?? 0;
            return hash;
        }
    }
}