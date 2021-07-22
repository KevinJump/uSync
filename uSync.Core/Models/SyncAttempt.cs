using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.Core.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public struct SyncAttempt<TObject>
    {
        /// <summary>
        ///  Attempt was successfull
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        ///  Name of the item 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  reference to the item
        /// </summary>
        public TObject Item { get; private set; }

        /// <summary>
        ///  object type for the item
        /// </summary>
        public string ItemType { get; private set; }

        /// <summary>
        ///  type of change that was performed
        /// </summary>
        public ChangeType Change { get; private set; }

        /// <summary>
        ///  additional message to display with attempt
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///  exception that might have occured
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        ///  Details of the changes that will/have happen(d)
        /// </summary>
        public IEnumerable<uSyncChange> Details { get; set; }


        /// <summary>
        ///  was the item saved within the serializer (stops double saving).
        /// </summary>
        public bool Saved { get; set; }
        private SyncAttempt(bool success, string name, TObject item, string itemType, ChangeType change,
            string message, Exception ex, bool saved)
            : this()
        {
            Success = success;
            Name = name;
            Item = item;
            ItemType = itemType;
            Change = change;
            Message = message;
            Exception = ex;
            Saved = saved;
        }

        // default object success (when we don't pass the item back)

        public static SyncAttempt<TObject> Succeed(string name, ChangeType change)
            => new SyncAttempt<TObject>(true, name, default(TObject), typeof(TObject).Name, change, string.Empty, null, false);

        public static SyncAttempt<TObject> Succeed(string name, ChangeType change, string message)
            => new SyncAttempt<TObject>(true, name, default(TObject), typeof(TObject).Name, change, message, null, false);


        // item passed back versions 
        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change)
            => new SyncAttempt<TObject>(true, name, item, typeof(TObject).Name, change, string.Empty, null, false);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, IList<uSyncChange> details)
        {
            var attempt = new SyncAttempt<TObject>(true, name, item, typeof(TObject).Name, change, string.Empty, null, false);
            attempt.Details = details;
            return attempt;
        }

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, bool saved)
            => Succeed(name, item, change, saved, null);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, bool saved, IList<uSyncChange> details)
        {
            var attempt = new SyncAttempt<TObject>(true, name, item, typeof(TObject).Name, change, string.Empty, null, saved);
            if (details != null) attempt.Details = details;
            return attempt;
        }

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, string message)
            => new SyncAttempt<TObject>(true, name, item, typeof(TObject).Name, change, message, null, false);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, string message, bool saved)
            => Succeed(name, item, change, message, saved, null);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, string message, bool saved, IList<uSyncChange> details)
        {
            var attempt = new SyncAttempt<TObject>(true, name, item, typeof(TObject).Name, change, message, null, saved);
            if (details != null) attempt.Details = details;
            return attempt;
        }

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject).Name, change, string.Empty, null, false);

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, string message)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject).Name, change, message, null, false);

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, string message, Exception ex)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject).Name, change, message, ex, false);

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, Exception ex)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject).Name, change, string.Empty, ex, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, string message)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject).Name, change, message, null, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, string message, Exception ex)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject).Name, change, message, ex, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, Exception ex)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject).Name, change, string.Empty, ex, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject).Name, change, string.Empty, null, false);

        public static SyncAttempt<TObject> SucceedIf(bool condition, string name, TObject item, ChangeType change)
            => new SyncAttempt<TObject>(condition, name, item, typeof(TObject).Name, change, string.Empty, null, false);

        // xelement ones, pass type
        public static SyncAttempt<TObject> Succeed(string name, TObject item, Type itemType, ChangeType change)
            => new SyncAttempt<TObject>(true, name, item, itemType.Name, change, string.Empty, null, false);

        public static SyncAttempt<TObject> Fail(string name, Type itemType, ChangeType change, string message)
            => new SyncAttempt<TObject>(false, name, default(TObject), itemType.Name, change, message, null, false);

        public static SyncAttempt<TObject> Fail(string name, Type itemType, ChangeType change, string message, Exception ex)
            => new SyncAttempt<TObject>(false, name, default(TObject), itemType.Name, change, message, ex, false);

        public static SyncAttempt<TObject> SucceedIf(bool condition, string name, TObject item, Type itemType, ChangeType change)
            => new SyncAttempt<TObject>(condition, name, item, itemType.Name, change, string.Empty, null, false);
    }

}