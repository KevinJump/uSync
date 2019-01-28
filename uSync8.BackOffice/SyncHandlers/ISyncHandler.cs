using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;

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
        Dictionary<string, string> Settings { get; set; }

        void InitializeEvents();

        IEnumerable<uSyncAction> ExportAll(string folder, uSyncHandlerSettings setting);
        IEnumerable<uSyncAction> ImportAll(string folder, bool force, uSyncHandlerSettings setting);
        IEnumerable<uSyncAction> Report(string folder, uSyncHandlerSettings setting);

        // uSyncAction Import(string file, bool force);

    }
}
