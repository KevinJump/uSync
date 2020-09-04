using System.Collections.Generic;

using Newtonsoft.Json;

using uSync.Core.Models;

namespace uSync.Core
{
    public static class ChangeListExtensions
    {
        public static void AddNew(this List<uSyncChange> changes, string name, string value, string path)
        {
            changes.Add(uSyncChange.Create(path, name, value));
        }


        public static void AddUpdate<TObject>(this List<uSyncChange> changes, string name, TObject oldValue, TObject newValue, string path = "")
            => AddUpdate(changes, name, oldValue.ToString(), newValue.ToString(), path);

        public static void AddUpdate(this List<uSyncChange> changes, string name, string oldValue, string newValue, string path = "")
        {
            changes.Add(uSyncChange.Update(path, name, oldValue, newValue));
        }

        public static void AddUpdateJson(this List<uSyncChange> changes, string name, object oldValue, object newValue, string path = "")
        {
            var oldJson = JsonConvert.SerializeObject(oldValue, Formatting.Indented);
            var newJson = JsonConvert.SerializeObject(newValue, Formatting.Indented);
            AddUpdate(changes, name, oldJson, newJson, path);
        }
    }
}
