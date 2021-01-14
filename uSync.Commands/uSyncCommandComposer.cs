using System;

using Umbraco.Core;
using Umbraco.Core.Composing;

using uSync8.BackOffice.Commands;

namespace uSync.BaseCommands
{
    /// <summary>
    ///  when the site won't boot, we register
    ///  just the Init and Quit Commands,
    /// </summary>
    [RuntimeLevel(MinLevel = RuntimeLevel.BootFailed, MaxLevel = RuntimeLevel.Install)]
    public class uSyncCommandComposer : IComposer
    {
        public void Compose(Composition composition)
        {
            // things we register when in pre-setup phase (so umbraco insn't setup).
            composition.RegisterUnique<SyncUserHelper>();

            // init - sets up db, user, installs umbraco. 
            composition.RegisterUnique<InitCommand>();

            // quit. 
            composition.RegisterUnique<QuitCommand>();
        }
    }


    [ComposeAfter(typeof(uSync8.BackOffice.uSyncBackOfficeComposer))]
    public class uSyncCommandBootedComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.RegisterUnique<SyncUserHelper>();

            composition.WithCollectionBuilder<SyncCommandCollectionBuilder>()
             .Add(() => composition.TypeLoader.GetTypes<ISyncCommand>());

            composition.RegisterUnique<SyncCommandFactory>();
        }
    }
}
