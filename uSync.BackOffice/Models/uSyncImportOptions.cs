using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using uSync.Core.Serialization;

namespace uSync.BackOffice
{
    /// <summary>
    ///  options passed to an import, report or export of an item.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncImportOptions
    {
        public Guid ImportId { get; set; }
        public string HandlerSet { get; set; }
        public SerializerFlags Flags { get; set; }

        public Dictionary<string, string> Settings { get; set; }

        public uSyncCallbacks Callbacks { get; set; }

        public string RootFolder { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncPagedImportOptions : uSyncImportOptions
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int ProgressMin { get; set; }
        public int ProgressMax { get; set; }

    }
}
