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
    /// <summary>
    ///  A uSyncAction details what just happed when an Handler did something to an item
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public struct uSyncAction
    {
        /// <summary>
        ///  Alias of the handler 
        /// </summary>
        public string HandlerAlias { get; set; }

        /// <summary>
        ///  Was the action a success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///  Type name of the item the action was performed on
        /// </summary>
        public string ItemType { get; set; }

        /// <summary>
        ///  message to display along with action
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///  exception encountered during action
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        ///  type of change performed 
        /// </summary>
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public ChangeType Change { get; set; }

        /// <summary>
        ///  path name for the uSync file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///  display name of the item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  9.2 a nice path for the thing (displayed).
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///  this action still requires some processing 
        /// </summary>
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

        /// <summary>
        ///  the GUID key value of the item 
        /// </summary>
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

        /// <summary>
        ///  Create a uSync action with the supplied details. 
        /// </summary>
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

        /// <summary>
        ///  create a fail uSyncAction object
        /// </summary>
        [Obsolete("Pass handler type with fail - Will remove in v13")]
        public static uSyncAction Fail(string name, string type, ChangeType change, string message, Exception ex)
            => new uSyncAction(false, name, type, change, message, ex, string.Empty);

        /// <summary>
        ///  create a fail uSyncAction object
        /// </summary>
        public static uSyncAction Fail(string name, string handlerType, string itemType, ChangeType change, string message, Exception ex)
            => new uSyncAction(false, name, itemType, change, message, ex, string.Empty, handlerType);


    }

    /// <summary>
    /// uSync action extensions 
    /// </summary>
    public struct uSyncActionHelper<T>
    {
        /// <summary>
        ///  Create a new uSyncAction from a SyncAttempt
        /// </summary>
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

        /// <summary>
        ///  Create a new report action
        /// </summary>
        [Obsolete("Reporting with the Path gives better feedback to the user.")]
        public static uSyncAction ReportAction(ChangeType changeType, string name, string file, Guid key, string handlerAlias, string message)
        {
            return new uSyncAction(true, name, typeof(T).Name, changeType, message, null, file, handlerAlias)
            {
                key = key
            };
        }

        /// <summary>
        ///  Create a new report action
        /// </summary>
        public static uSyncAction ReportAction(ChangeType changeType, string name, string path, string file, Guid key, string handlerAlias, string message)
        {
            return new uSyncAction(true, name, typeof(T).Name, changeType, message, null, file, handlerAlias)
            {
                key = key,
                Path = path
            };
        }

        /// <summary>
        ///  Create a new failed report action
        /// </summary>
        public static uSyncAction ReportActionFail(string name, string message)
            => new uSyncAction(false, name, typeof(T).Name, ChangeType.Fail, message, null, string.Empty);
    }
}
