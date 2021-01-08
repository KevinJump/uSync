using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace uSync8.Core.Models
{
    /// <summary>
    ///  tracks the details of a change to an individual item
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncChange
    {
        public bool Success { get; set; } = true;

        /// <summary>
        ///  Name of item/property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  reference path to property
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///  old value (pre change)
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        ///  new value (after change)
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        ///  Change type
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeDetailType Change { get; set; }

        public static uSyncChange Create(string path, string name, string newValue, bool useNew = true)
            => new uSyncChange
            {
                Change = ChangeDetailType.Create,
                Path = path,
                Name = name,
                OldValue = "",
                NewValue = useNew ? newValue : "New Property"
            };

        public static uSyncChange Delete(string path, string name, string oldValue, bool useOld = true)
            => new uSyncChange
            {
                Change = ChangeDetailType.Delete,
                Path = path,
                Name = name,
                OldValue = useOld ? oldValue : "Missing Property",
                NewValue = ""
            };

        public static uSyncChange Update(string path, string name, string oldValue, string newValue, bool success = true)
            => new uSyncChange
            {
                Success = success,
                Name = name,
                Path = path,
                Change = ChangeDetailType.Update,
                NewValue = string.IsNullOrEmpty(newValue) ? "(Blank)" : newValue,
                OldValue = string.IsNullOrEmpty(oldValue) ? "(Blank)" : oldValue
            };

        public static uSyncChange Update(string path, string name, IEnumerable<string> oldValues, IEnumerable<string> newValues)
            => Update(path, name, string.Join(",", oldValues), string.Join(",", newValues));

        public static uSyncChange Update<TObject>(string path, string name, TObject oldValue, TObject newValue)
            => Update(path, name, oldValue.ToString(), newValue.ToString());

        public static uSyncChange NoChange(string path, string name)
            => new uSyncChange
            {
                Name = name,
                Path = path,
                Change = ChangeDetailType.NoChange
            };

        public static uSyncChange Error(string path, string name, string oldValue)
            => new uSyncChange
            {
                Name = name,
                Path = path,
                OldValue = oldValue,
                Change = ChangeDetailType.Error
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
