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
        string Name { get; }
        int Priority { get; }
        string DefaultFolder { get; }

        void InitializeEvents();

        IEnumerable<uSyncAction> ExportAll(string folder);
        IEnumerable<uSyncAction> ImportAll(string folder, bool force);
        IEnumerable<uSyncAction> Report(string folder);

        // uSyncAction Import(string file, bool force);
        bool Enabled { get; }

    }
}
