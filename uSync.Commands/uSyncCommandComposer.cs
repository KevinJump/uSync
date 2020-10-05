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
            Console.WriteLine("Register for pre boot");

            composition.RegisterUnique<InitCommand>();
            composition.RegisterUnique<QuitCommand>();
        }
    }


    [ComposeAfter(typeof(uSync8.BackOffice.uSyncBackOfficeComposer))]
    public class uSyncCommandBootedComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.WithCollectionBuilder<SyncCommandCollectionBuilder>()
             .Add(() => composition.TypeLoader.GetTypes<ISyncCommand>());

            composition.RegisterUnique<SyncCommandFactory>();
        }
    }
}
