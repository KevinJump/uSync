using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core.Models
{
    public class uSyncChange
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public string OldValue { get; set; }
        public string NewValue { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeDetailType Change { get; set; }
    }

    public enum ChangeDetailType
    {
        NoChange,
        Create,
        Update,
        Delete,
        Error
    }

}
