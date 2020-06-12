using System.Net.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace uSync8.Core.Models
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncChange
    {
        public string Name { get; private set; }
        public string Path { get; private set; }

        public string OldValue { get; private set; }
        public string NewValue { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeDetailType Change { get; private set; }

        public static uSyncChange Create(string path, string name, string newValue, bool useNew = true)
            => CreateChange(ChangeDetailType.Create, path, newValue, "", useNew ? newValue : "New property");

        public static uSyncChange Delete(string path, string name, string oldValue, bool useOld = true)
            => CreateChange(ChangeDetailType.Delete, path, name, useOld ? oldValue : "Missing property", "");

        public static uSyncChange Update(string path, string name, string oldValue, string newValue)
            => CreateChange(ChangeDetailType.Update, path, name, oldValue, newValue);

        public static uSyncChange Error(string path, string name, string oldValue, string newValue)
            => CreateChange(ChangeDetailType.Error, path, name, oldValue, newValue);

        public static uSyncChange Update<TObject>(string path, string name, TObject oldValue, TObject newValue)
            => Update(path, name, oldValue.ToString(), newValue.ToString());

        private static uSyncChange CreateChange(ChangeDetailType change, string path, string name, string oldValue, string newValue)
            => new uSyncChange
            {
                Change = change,
                Path = path,
                Name = name,
                NewValue = string.IsNullOrEmpty(newValue) ? "(Blank)" : newValue,
                OldValue = string.IsNullOrEmpty(oldValue) ? "(Blank)" : newValue
            };

        public static uSyncChange NoChange(string path, string name)
            => new uSyncChange
            {
                Name = name,
                Path = path,
                Change = ChangeDetailType.NoChange
            };
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
