﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using uSync8.Core;
using uSync8.Core.Models;

namespace uSync8.BackOffice
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]

    public struct uSyncAction
    {
        public string HandlerAlias { get; set; }

        public bool Success { get; set; }
        public Type ItemType { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeType Change { get; set; }

        public string FileName { get; set; }
        public string Name { get; set; }
        public bool RequiresPostProcessing { get; set; }

        /// <summary>
        ///  text that is shown on the details screen above any details. 
        /// </summary>
        public string DetailMessage { get; set; }

        /// <summary>
        ///  list of detailed changes, so you can see what is changing.
        /// </summary>
        public IEnumerable<uSyncChange> Details { get; set; }

        public Guid key { get; set; }

        internal uSyncAction(bool success, string name, Type type, ChangeType change, string message, Exception ex, string filename, string handlerAlias, bool postProcess = false) : this()
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

        internal uSyncAction(bool success, string name, Type type, ChangeType change, string message, Exception ex, string filename, bool postProcess = false)
            : this(success, name, type, change, message, ex, filename, null, postProcess)
        { }


        public static uSyncAction SetAction(
            bool success,
            string name,
            string handlerAlias,
            Type type = null,
            ChangeType change = ChangeType.NoChange,
            string message = null,
            Exception ex = null,
            string filename = null)
        {
            return new uSyncAction(success, name, type, change, message, ex, filename, handlerAlias);
        }

        public static uSyncAction SetAction(
            bool success,
            string name,
            Type type = null,
            ChangeType change = ChangeType.NoChange,
            string message = null,
            Exception ex = null,
            string filename = null)
        {
            return new uSyncAction(success, name, type, change, message, ex, filename);
        }

        public static uSyncAction Fail(string name,
            Type type,
            ChangeType change = ChangeType.Fail,
            string message = null,
            Exception ex = null,
            string filename = null)
        {
            return new uSyncAction(false, name, type, change, message, null, string.Empty);
        }


        public static uSyncAction Fail(string name, Type type, string message)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, message, null, string.Empty);
        }


        public static uSyncAction Fail(string name, Type type, ChangeType change, string message)
        {
            return new uSyncAction(false, name, type, change, message, null, string.Empty);
        }

        public static uSyncAction Fail(string name, Type type, string message, string file)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, message, null, file);
        }

        public static uSyncAction Fail(string name, Type type, Exception ex)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, string.Empty, ex, string.Empty);
        }

        public static uSyncAction Fail(string name, Type type, ChangeType change, Exception ex)
        {
            return new uSyncAction(false, name, type, change, string.Empty, ex, string.Empty);
        }

        public static uSyncAction Fail(string name, Type type, Exception ex, string file)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, string.Empty, ex, file);
        }
    }

    public struct uSyncActionHelper<T>
    {

        public static uSyncAction SetAction(SyncAttempt<T> attempt, string filename, string handlerAlias, bool requirePostProcessing = true)
        {
            return new uSyncAction(attempt.Success, attempt.Name, attempt.ItemType, attempt.Change, attempt.Message, attempt.Exception, filename, handlerAlias, requirePostProcessing);
        }

        public static uSyncAction SetAction(SyncAttempt<T> attempt, string filename, bool requirePostProcessing = true)
        {
            return new uSyncAction(attempt.Success, attempt.Name, attempt.ItemType, attempt.Change, attempt.Message, attempt.Exception, filename, requirePostProcessing);
        }

        public static uSyncAction ReportAction(ChangeType changeType, string name)
        {
            return new uSyncAction(true, name, typeof(T), changeType, string.Empty, null, string.Empty);
        }

        public static uSyncAction ReportAction(ChangeType changeType, string name, string file, Guid key, string handlerAlias)
        {
            return new uSyncAction(true, name, typeof(T), changeType, string.Empty, null, file, handlerAlias)
            {
                key = key
            };
        }

        public static uSyncAction ReportAction(bool willUpdate, string name, string message)
        {
            return new uSyncAction(true, name, typeof(T),
                willUpdate ? ChangeType.Update : ChangeType.NoChange,
                message, null, string.Empty);
        }

        public static uSyncAction ReportAction(bool willUpdate, string name, string message, string handlerAlias)
        {
            return new uSyncAction(true, name, typeof(T),
                willUpdate ? ChangeType.Update : ChangeType.NoChange,
                message, null, string.Empty, handlerAlias);

        }

        public static uSyncAction ReportActionFail(string name, string message)
        {
            return new uSyncAction(false, name, typeof(T), ChangeType.Fail, message, null, string.Empty);
        }
    }
}
