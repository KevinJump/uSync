using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    public interface ISyncHandler
    {
        string Alias { get; }
        string Name { get; }
        int Priority { get; }
        string DefaultFolder { get; }
        string Icon { get; }
        Type ItemType { get; }

        bool Enabled { get; set; }
        HandlerSettings DefaultConfig { get; set; }

        void Initialize(HandlerSettings settings);

        IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings settings);
        IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings settings, bool force);
        IEnumerable<uSyncAction> Report(string folder, HandlerSettings settings);

        // uSyncAction Import(string file, bool force);

    }
}
