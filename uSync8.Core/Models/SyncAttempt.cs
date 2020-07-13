using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;

namespace uSync8.Core.Models
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
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
        public Type ItemType { get; private set; }

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
        private SyncAttempt(bool success, string name, TObject item, Type itemType, ChangeType change,
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

        public static SyncAttempt<TObject> Succeed(string name, ChangeType change)
                    => new SyncAttempt<TObject>(true, name, default(TObject), typeof(TObject), change, string.Empty, null, false);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change)
            => new SyncAttempt<TObject>(true, name, item, typeof(TObject), change, string.Empty, null, false);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, bool saved)
            => new SyncAttempt<TObject>(true, name, item, typeof(TObject), change, string.Empty, null, saved);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, string message)
            => new SyncAttempt<TObject>(true, name, item, typeof(TObject), change, message, null, false);

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, string message, bool saved)
            => new SyncAttempt<TObject>(true, name, item, typeof(TObject), change, message, null, saved);

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, string.Empty, null, false);

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, string message)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, message, null, false);

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, string message, Exception ex)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, message, ex, false);

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, Exception ex)
            => new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, string.Empty, ex, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, string message)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, message, null, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, string message, Exception ex)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, message, ex, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, Exception ex)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, string.Empty, ex, false);

        public static SyncAttempt<TObject> Fail(string name, ChangeType change)
            => new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, string.Empty, null, false);

        public static SyncAttempt<TObject> SucceedIf(bool condition, string name, TObject item, ChangeType change)
            => new SyncAttempt<TObject>(condition, name, item, typeof(TObject), change, string.Empty, null, false);

        // xelement ones, pass type
        public static SyncAttempt<TObject> Succeed(string name, TObject item, Type itemType, ChangeType change)
            => new SyncAttempt<TObject>(true, name, item, itemType, change, string.Empty, null, false);

        public static SyncAttempt<TObject> Fail(string name, Type itemType, ChangeType change, string message)
            => new SyncAttempt<TObject>(false, name, default(TObject), itemType, change, message, null, false);

        public static SyncAttempt<TObject> Fail(string name, Type itemType, ChangeType change, string message, Exception ex)
            => new SyncAttempt<TObject>(false, name, default(TObject), itemType, change, message, ex, false);

        public static SyncAttempt<TObject> SucceedIf(bool condition, string name, TObject item, Type itemType, ChangeType change)
            => new SyncAttempt<TObject>(condition, name, item, itemType, change, string.Empty, null, false);
    }

}