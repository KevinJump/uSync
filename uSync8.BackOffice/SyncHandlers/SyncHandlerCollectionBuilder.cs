using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
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
        /// <summary>
        ///  handlers that impliment the Extended Handler interface, can be used for other things.
        /// </summary>
        private List<ISyncExtendedHandler> extendedHandlers;

        public SyncHandlerCollection(IEnumerable<ISyncHandler> items)
            : base(items)
        {
            extendedHandlers = items
                .Where(x => x is ISyncExtendedHandler)
                .Select(x => x as ISyncExtendedHandler)
                .ToList();
        }

        public IEnumerable<ISyncHandler> Handlers => this;

        public IEnumerable<ISyncExtendedHandler> ExtendedHandlers
            => extendedHandlers;

        // v8.1
        [Obsolete("Use Handler Factory for better results")]
        public IEnumerable<HandlerConfigPair> GetValidHandlers(string actionName, uSyncSettings settings)
        {
            var configPairs = new List<HandlerConfigPair>();

            foreach(var handler in this)
            {
                var config = settings.DefaultHandlerSet().Handlers.FirstOrDefault(x => x.Alias.InvariantEquals(handler.Alias));
                if (config == null)
                {
                    config = new HandlerSettings(handler.Alias, false)
                    {
                        GuidNames = new OverriddenValue<bool>(settings.UseGuidNames, false),
                        UseFlatStructure = new OverriddenValue<bool>(settings.UseFlatStructure, false)
                    };
                }

                if (config != null && config.Enabled)
                {
                    configPairs.Add(new HandlerConfigPair() { Handler = handler, Settings = config });
                }
            }

            return configPairs.OrderBy(x => x.Handler.Priority);
        }
    }
}
