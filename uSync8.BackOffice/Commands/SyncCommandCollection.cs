using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core.Composing;

namespace uSync8.BackOffice.Commands
{
    public class SyncCommandCollection
        : BuilderCollectionBase<ISyncCommand>
    {
        public SyncCommandCollection(IEnumerable<ISyncCommand> items) 
            : base(items)
        {  }

        public ISyncCommand GetCommand(string alias)
            => this.FirstOrDefault(x => x.Alias.Equals(alias, StringComparison.InvariantCultureIgnoreCase));
    }

    public class SyncCommandCollectionBuilder
        : LazyCollectionBuilderBase<SyncCommandCollectionBuilder, SyncCommandCollection, ISyncCommand>
    {
        protected override SyncCommandCollectionBuilder This => this;
    }
}
