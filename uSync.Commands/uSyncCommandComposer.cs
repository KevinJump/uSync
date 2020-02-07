﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using uSync8.BackOffice.Commands;

namespace uSync.Commands
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
