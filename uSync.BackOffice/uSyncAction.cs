using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public struct uSyncAction
    {
        public string HandlerAlias { get; set; }

        public bool Success { get; set; }

        public string ItemType { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public ChangeType Change { get; set; }

        public string FileName { get; set; }
        public string Name { get; set; }

        /// <summary>
        ///  9.2 a nice path for the thing (displayed).
        /// </summary>
        public string Path { get; set; }
        public bool RequiresPostProcessing { get; set; }

        /// <summary>
        ///  boxed item - used on updates. 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public object Item { get; set; }

        /// <summary>
        ///  text that is shown on the details screen above any details. 
        /// </summary>
        public string DetailMessage { get; set; }

        /// <summary>
        ///  list of detailed changes, so you can see what is changing.
        /// </summary>
        public IEnumerable<uSyncChange> Details { get; set; }

        public Guid key { get; set; }

        internal uSyncAction(bool success, string name, string type, ChangeType change, string message, Exception ex, string filename, string handlerAlias, bool postProcess = false) : this()
        {
            Success = success;
            Name = name;
            ItemType = type;
            Message = message;
            Change = change;
            Exception = ex;
            FileName = filename;
            RequiresPostProcessing = postProcess;

            HandlerAlias = handlerAlias;
            key = Guid.Empty;

        }

        internal uSyncAction(bool success, string name, string type, ChangeType change, string message, Exception ex, string filename, bool postProcess = false)
            : this(success, name, type, change, message, ex, filename, null, postProcess)
        { }

        public static uSyncAction SetAction(
            bool success,
            string name,
            string type = "",
            ChangeType change = ChangeType.NoChange,
            string message = null,
            Exception ex = null,
            string filename = null)
        {
            return new uSyncAction(success, name, type, change, message, ex, filename);
        }

        public static uSyncAction Fail(string name, string type, ChangeType change, string message, Exception ex)
            => new uSyncAction(false, name, type, change, message, ex, string.Empty);
    }

    public struct uSyncActionHelper<T>
    {
        public static uSyncAction SetAction(SyncAttempt<T> attempt, string filename, Guid key, string handlerAlias, bool requirePostProcessing = true)
        {
            var action = new uSyncAction(attempt.Success, attempt.Name, attempt.ItemType, attempt.Change, attempt.Message, attempt.Exception, filename, handlerAlias, requirePostProcessing);
            action.key = key;
            if (attempt.Details != null && attempt.Details.Any())
            {
                action.Details = attempt.Details;
            }
            return action;
        }

        [Obsolete("Reporting with the Path gives better feedback to the user.")]
        public static uSyncAction ReportAction(ChangeType changeType, string name, string file, Guid key, string handlerAlias, string message)
        {
            return new uSyncAction(true, name, typeof(T).Name, changeType, message, null, file, handlerAlias)
            {
                key = key
            };
        }

        public static uSyncAction ReportAction(ChangeType changeType, string name, string path, string file, Guid key, string handlerAlias, string message)
        {
            return new uSyncAction(true, name, typeof(T).Name, changeType, message, null, file, handlerAlias)
            {
                key = key,
                Path = path
            };
        }

        public static uSyncAction ReportActionFail(string name, string message)
            => new uSyncAction(false, name, typeof(T).Name, ChangeType.Fail, message, null, string.Empty);
    }
}
