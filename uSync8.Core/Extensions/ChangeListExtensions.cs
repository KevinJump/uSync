using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using uSync8.Core.Models;

namespace uSync8.Core
{
    public static class ChangeListExtensions
    {
        public static void AddNew(this List<uSyncChange> changes, string name, string value, string path)
        {
            changes.Add(new uSyncChange
            {
                Change = ChangeDetailType.Create,
                Name = name,
                NewValue = value,
                OldValue = string.Empty,
                Path = path
            });
        }


        public static void AddUpdate<TObject>(this List<uSyncChange> changes, string name, TObject oldValue, TObject newValue, string path = "")
            => AddUpdate(changes, name, oldValue.ToString(), newValue.ToString(), path);

        public static void AddUpdate(this List<uSyncChange> changes, string name, string oldValue, string newValue, string path = "")
        {
            changes.Add(new uSyncChange
            {
                Change = ChangeDetailType.Update,
                Name = name,
                NewValue = string.IsNullOrWhiteSpace(newValue) ? "(blank)" : newValue,
                OldValue = string.IsNullOrWhiteSpace(oldValue) ? "(blank)" : oldValue,
                Path = path,
            });
        }

        public static void AddUpdateJson(this List<uSyncChange> changes, string name, object oldValue, object newValue, string path = "")
        {
            var oldJson = JsonConvert.SerializeObject(oldValue, Formatting.Indented);
            var newJson = JsonConvert.SerializeObject(newValue, Formatting.Indented);
            AddUpdate(changes, name, oldJson, newJson, path);
        }
    }
}
