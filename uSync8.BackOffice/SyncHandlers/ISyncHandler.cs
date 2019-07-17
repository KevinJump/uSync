using System;
using System.Collections.Generic;

using uSync8.BackOffice.Configuration;

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
}
