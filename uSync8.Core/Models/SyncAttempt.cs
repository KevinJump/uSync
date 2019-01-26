using System;
using System.Collections.Generic;

namespace uSync8.Core.Models
{
    public struct SyncAttempt<TObject>
    {
        public bool Success { get; private set; }
        public string Name { get; set; }
        public TObject Item { get; private set; }
        public Type ItemType { get; private set; }
        public ChangeType Change { get; private set; }

        public string Message { get; private set; }
        public Exception Exception { get; set; }

        public IEnumerable<uSyncChange> Details { get; set; }

        private SyncAttempt(bool success, string name, TObject item, Type itemType, ChangeType change,
            string message, Exception ex)
            : this()
        {
            Success = success;
            Name = name;
            Item = item;
            ItemType = itemType;
            Change = change;
            Message = message;
            Exception = ex;
        }

        public static SyncAttempt<TObject> Succeed(string name, ChangeType change)
        {
            return new SyncAttempt<TObject>(true, name, default(TObject), typeof(TObject), change, string.Empty, null);
        }

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change)
        {
            return new SyncAttempt<TObject>(true, name, item, typeof(TObject), change, string.Empty, null);
        }

        public static SyncAttempt<TObject> Succeed(string name, TObject item, ChangeType change, string message)
        {
            return new SyncAttempt<TObject>(true, name, item, typeof(TObject), change, message, null);
        }

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change)
        {
            return new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, string.Empty, null);
        }

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, string message)
        {
            return new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, message, null);
        }

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, message, ex);
        }

        public static SyncAttempt<TObject> Fail(string name, TObject item, ChangeType change, Exception ex)
        {
            return new SyncAttempt<TObject>(false, name, item, typeof(TObject), change, string.Empty, ex);
        }

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, string message)
        {
            return new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, message, null);
        }

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, message, ex);
        }

        public static SyncAttempt<TObject> Fail(string name, ChangeType change, Exception ex)
        {
            return new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, string.Empty, ex);
        }

        public static SyncAttempt<TObject> Fail(string name, ChangeType change)
        {
            return new SyncAttempt<TObject>(false, name, default(TObject), typeof(TObject), change, string.Empty, null);
        }

        public static SyncAttempt<TObject> SucceedIf(bool condition, string name, TObject item, ChangeType change)
        {
            return new SyncAttempt<TObject>(condition, name, item, typeof(TObject), change, string.Empty, null);
        }


        // xelement ones, pass type
        //
        public static SyncAttempt<TObject> Succeed(string name, TObject item, Type itemType, ChangeType change)
        {
            return new SyncAttempt<TObject>(true, name, item, itemType, change, string.Empty, null);
        }

        public static SyncAttempt<TObject> Fail(string name, Type itemType, ChangeType change, string message)
        {
            return new SyncAttempt<TObject>(false, name, default(TObject), itemType, change, message, null);
        }

        public static SyncAttempt<TObject> Fail(string name, Type itemType, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<TObject>(false, name, default(TObject), itemType, change, message, ex);
        }

        public static SyncAttempt<TObject> SucceedIf(bool condition, string name, TObject item, Type itemType, ChangeType change)
        {
            return new SyncAttempt<TObject>(condition, name, item, itemType, change, string.Empty, null);
        }

    }

}