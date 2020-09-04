using Umbraco.Core;
using Umbraco.Core.Composing;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace uSync.BackOffice
{
    [ComposeAfter(typeof(uSyncCoreComposer))]
    public class uSyncBackOfficeConfigComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Configs.Add<uSyncConfig>(() => new uSyncConfig(composition.Logger));
        }
    }

    [ComposeAfter(typeof(uSyncBackOfficeConfigComposer))]
    public class uSyncBackOfficeComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {

            composition.RegisterUnique<SyncFileService>();

            composition.WithCollectionBuilder<SyncHandlerCollectionBuilder>()
                .Add(() => composition.TypeLoader.GetTypes<ISyncHandler>());

            composition.RegisterUnique<SyncHandlerFactory>();

            composition.RegisterUnique<uSyncService>();

            composition.Components().Append<uSyncBackofficeComponent>();
        }
    }
}
