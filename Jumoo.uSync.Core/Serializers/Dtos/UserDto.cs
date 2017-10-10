using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Jumoo.uSync.Core.Serializers.Dtos
{
    [Serializable]
    [XmlType(TypeName = "User")]
    public class UserDto
    { 
        public UserGeneralSection General { get; set; }
        [XmlArrayItem("AllowedSection")]
        public List<String> AllowedSections { get; set; }
        public List<NodePermission> NodePermissions { get; set; }

        public override bool Equals(object obj)
        {
            var anotherDto = obj as UserDto;
            if (anotherDto == null) return false;
            return GetHashCode() == anotherDto.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = General?.GetHashCode() ?? 0;
            AllowedSections?.ForEach(s =>
            {
                hash = (hash * 7) + s.GetHashCode();
            });
            NodePermissions?.ForEach(s =>
            {
                hash = (hash * 7) + s.GetHashCode();
            });
            return hash;
        }
    }

    [Serializable]
    public class UserGeneralSection : GeneralSectionBase
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Language { get; set; }
        public bool UserEnabled { get; set; }
        public bool UmbracoAccessDisabled { get; set; }
        public Guid StartContentNodeKey { get; set; }
        public Guid StartMediaNodeKey { get; set; }
        public string UserTypeAlias { get; set; }
        public string RawPassword { get; set; }
        public string RawPasswordAnswer { get; set; }
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();        
            hash = (hash * 7) + Email?.GetHashCode() ?? 0;
            hash = (hash * 7) + Language?.GetHashCode() ?? 0;
            hash = (hash * 7) + UserTypeAlias?.GetHashCode() ?? 0;
            hash = (hash * 7) + UmbracoAccessDisabled.GetHashCode();
            hash = (hash * 7) + UserEnabled.GetHashCode();
            hash = (hash * 7) + StartContentNodeKey.GetHashCode();
            hash = (hash * 7) + StartMediaNodeKey.GetHashCode();
            return hash;
        }
    }

    [Serializable]
    public class NodePermission
    {
        [XmlAttribute(AttributeName = "nodeKey")]
        public Guid NodeKey { get; set; }
        [XmlElement(ElementName = "Permission")]
        public List<string> Permissions { get; set; }
        public override int GetHashCode()
        {
            int hash = NodeKey.GetHashCode();
            Permissions?.ForEach(s =>
            {
                hash = (hash * 7) + s.GetHashCode();
            });
            return hash;
        }
    }
}