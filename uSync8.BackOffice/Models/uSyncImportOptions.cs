using System;
using System.Collections.Generic;

using uSync8.BackOffice.Configuration;
using uSync8.Core.Serialization;

namespace uSync8.BackOffice
{
    /// <summary>
    ///  options passed to an import, report or export of an item.
    /// </summary>
    public class uSyncImportOptions
    {
        public Guid ImportId { get; set; }
        public string HandlerSet { get; set; }
        public SerializerFlags Flags { get; set; }

        public Dictionary<string, string> Settings { get; set; }

        public uSyncCallbacks Callbacks { get; set; }

        public string RootFolder { get; set; }
    }

    public class uSyncPagedImportOptions : uSyncImportOptions
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int ProgressMin { get; set; }
        public int ProgressMax { get; set; }
    
    }
}
