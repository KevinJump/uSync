using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    public class SyncHandlerCollectionBuilder
        : LazyCollectionBuilderBase<SyncHandlerCollectionBuilder, SyncHandlerCollection, ISyncHandler> 
    {
        protected override SyncHandlerCollectionBuilder This => this;
    }

    public class SyncHandlerCollection : BuilderCollectionBase<ISyncHandler>
    {
        public SyncHandlerCollection(IEnumerable<ISyncHandler> items)
            : base(items)
        {
        }

        public IEnumerable<HandlerConfigPair> GetValidHandlers(string actionName, uSyncSettings settings)
        {
            var validHandlers = new List<HandlerConfigPair>();

            foreach (var syncHandler in this)
            {
                var config = settings.Handlers.FirstOrDefault(x => x.Alias.InvariantEquals(syncHandler.Alias));
                if (config == null)
                {
                    config = new HandlerSettings(syncHandler.Alias, settings.EnableMissingHandlers)
                    {
                        GuidNames = new OverriddenValue<bool>(settings.UseGuidNames, false),
                        UseFlatStructure = new OverriddenValue<bool>(settings.UseFlatStructure, false)
                    };
                }

                if (config != null && config.Enabled)
                {
                    validHandlers.Add(new HandlerConfigPair()
                    {
                        Handler = syncHandler,
                        Settings = config
                    });
                }
            }

            return validHandlers;
        }
    }

    public class HandlerConfigPair
    {
        public ISyncHandler Handler { get; set; }
        public HandlerSettings Settings { get; set; }
    }
}
