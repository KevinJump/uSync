using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Models.Membership;

namespace Jumoo.uSync.Core.Entities
{
    [Serializable]
    [DataContract(IsReference = true)]
    public class UserType : Entity, IUserType
    {
        [DataMember]
        public string Alias { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public IEnumerable<string> Permissions { get; set; }
    }
}