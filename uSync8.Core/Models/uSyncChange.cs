using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
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

        public static uSyncChange Create(string path, string name, string newValue, bool useNew = true)
        {
            return new uSyncChange()
            {
                Change = ChangeDetailType.Create,
                Path = path,
                Name = name,
                OldValue = "",
                NewValue = useNew ? newValue : "New Property"
            };
        }

        public static uSyncChange Delete(string path, string name, string oldValue, bool useOld = true)
        {
            return new uSyncChange()
            {
                Change = ChangeDetailType.Delete,
                Path = path,
                Name = name,
                OldValue = useOld ? oldValue : "Missing Property",
                NewValue = ""
            };
        }

        public static uSyncChange Update(string path, string name, string oldValue, string newValue)
        {
            return new uSyncChange()
            {
                Name = name,
                Path = path,
                Change = ChangeDetailType.Update,
                NewValue = string.IsNullOrEmpty(newValue) ? "(Blank)" : newValue,
                OldValue = string.IsNullOrEmpty(oldValue) ? "(Blank)" : oldValue
            };
        }

        public static uSyncChange Update<TObject>(string path, string name, TObject oldValue, TObject newValue)
        {
            return Update(path, name, oldValue.ToString(), newValue.ToString());
        }
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
