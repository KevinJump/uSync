using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;

namespace uSync8.BackOffice
{
    [ComposeAfter(typeof(uSyncCoreComposer))]
    public class uSyncBackOfficeComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Configs.Add<uSyncConfig>(() => new uSyncConfig());

            composition.RegisterUnique<SyncFileService>();

            composition.WithCollectionBuilder<SyncHandlerCollectionBuilder>()
                .Add(() => composition.TypeLoader.GetTypes<ISyncHandler>());

            composition.Components().Append<uSyncBackofficeComponent>();

            composition.RegisterUnique<uSyncService>();
        }
    }
}
