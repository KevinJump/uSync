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
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncImportOptions
    {
        /// <summary>
        /// Unique ID for this series of import operations
        /// </summary>
        public Guid ImportId { get; set; }

        /// <summary>
        /// Handler set to load for operation
        /// </summary>
        public string HandlerSet { get; set; }

        /// <summary>
        /// Flags to pass to the serializers 
        /// </summary>
        public SerializerFlags Flags { get; set; }

        /// <summary>
        /// Additional settings on the handlers/serializers for this operation
        /// </summary>
        public Dictionary<string, string> Settings { get; set; }

        /// <summary>
        /// SignalR callbacks to use for UI communication
        /// </summary>
        public uSyncCallbacks Callbacks { get; set; }

        /// <summary>
        /// Root folder for all uSync operations
        /// </summary>
        public string RootFolder { get; set; }

        /// <summary>
        /// (reserved for future use)
        /// </summary>
        public bool EnableRollback { get; set; }


        /// <summary>
        ///  should we pause the uSync export events during the import ?
        /// </summary>
        public bool PauseDuringImport { get; set; } = true;
    }

    /// <summary>
    ///  Import options when paging any import operations 
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncPagedImportOptions : uSyncImportOptions
    {
        /// <summary>
        /// Page number 
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Page size - number of items to process per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// progress bar lower bound value 
        /// </summary>
        public int ProgressMin { get; set; }

        /// <summary>
        /// progress bar upper bound value
        /// </summary>
        public int ProgressMax { get; set; }

        /// <summary>
        ///  should include what is a normally disabled handler when looking for 
        ///  something to process the folder.
        /// </summary>
        public bool IncludeDisabledHandlers { get; set; }

        /// <summary>
        ///  Pause the events, so export events don't fire when we import 
        /// </summary>
        /// <remarks>
        ///  default behaviour is to pause during an import, but if we don't 
        ///  want to (for exporter etc) this means we can keep the uSync folder
        ///  in sync because we will cause a default export when an import happens.
        /// </remarks>
        public bool PauseDuringImport { get; set; } = true; 

    }
}
