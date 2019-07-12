using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using uSync8.BackOffice.Configuration;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using static uSync8.BackOffice.uSyncService;

namespace uSync8.BackOffice.SyncHandlers
{
    public delegate void SyncUpdateCallback(string message, int count, int total);

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

        IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings settings, SyncUpdateCallback callback);
        IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings settings, bool force, SyncUpdateCallback callback);
        IEnumerable<uSyncAction> Report(string folder, HandlerSettings settings, SyncUpdateCallback callback);
    }

    public interface IGroupedSyncHandler
    {
        string Group { get; }
    }
}
