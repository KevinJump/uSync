using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.Extensions;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers
{
    public class SyncHandlerFactory
    {
        private SyncHandlerCollection syncHandlers;
        private uSyncSettings settings;
        private ILogger<SyncHandlerFactory> logger;

        private IOptionsMonitor<uSyncHandlerSetSettings> handlerSetSettingsAccessor;

        public SyncHandlerFactory(
            ILogger<SyncHandlerFactory> logger,
            SyncHandlerCollection syncHandlers,
            IOptionsMonitor<uSyncHandlerSetSettings> handlerSetSettingsAccessor,
            IOptionsMonitor<uSyncSettings> options)
        {
            this.handlerSetSettingsAccessor = handlerSetSettingsAccessor;
            this.logger = logger;
            this.syncHandlers = syncHandlers;
            this.settings = options.CurrentValue;
        }

        #region All getters (regardless of set or config)

        public IEnumerable<ISyncHandler> GetAll()
            => syncHandlers.Handlers;

        public ISyncHandler GetHandler(string alias)
            => syncHandlers.Handlers
                .FirstOrDefault(x => x.Alias.InvariantEquals(alias));

        public IEnumerable<ISyncHandler> GetHandlers(params string[] aliases)
            => syncHandlers.Where(x => aliases.InvariantContains(x.Alias));

        /// <summary>
        ///  returns the handler groups (settings, content, users, etc) that stuff can be grouped into
        /// </summary>
        public IEnumerable<string> GetGroups()
            => syncHandlers.Handlers
                .Select(x => x.Group)
                .Distinct();

        #endregion

        #region Default Config Loaders - for when you know exactly what you want

        /// <summary>
        ///  Get Default Handlers based on Alias
        /// </summary>
        /// <param name="aliases">aliases of handlers you want </param>
        /// <returns>Handler/Config Pair with default config loaded</returns>
        public IEnumerable<HandlerConfigPair> GetDefaultHandlers(IEnumerable<string> aliases)
            => GetAll()
                    .Where(x => aliases.InvariantContains(x.Alias))
                    .Select(x => new HandlerConfigPair()
                    {
                        Handler = x,
                        Settings = x.DefaultConfig
                    });

        #endregion


        #region Valid Loaders (need set, group, action)

        public HandlerConfigPair GetValidHandler(string alias, SyncHandlerOptions options = null)
             => GetValidHandlers(options)
                 .FirstOrDefault(x => x.Handler.Alias.InvariantEquals(alias));

        public HandlerConfigPair GetValidHandlerByTypeName(string typeName, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => typeName.InvariantEquals(x.Handler.TypeName))
                .FirstOrDefault();

        public HandlerConfigPair GetValidHandlerByEntityType(string entityType, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => x.Handler.EntityType.InvariantEquals(entityType))
                .FirstOrDefault();


        public IEnumerable<HandlerConfigPair> GetValidHandlersByEntityType(IEnumerable<string> entityTypes, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => entityTypes.InvariantContains(x.Handler.EntityType));


        public IEnumerable<string> GetValidGroups(SyncHandlerOptions options = null)
        {
            var handlers = GetValidHandlers(options);
            var groups = handlers
                .Select(x => x.Handler.Group)
                .ToList();

            groups.AddRange(handlers.Where(x => !string.IsNullOrWhiteSpace(x.Settings.Group))
                .Select(x => x.Settings.Group));

            return groups.Distinct();              
        }

        public IEnumerable<HandlerConfigPair> GetValidHandlers(string[] aliases, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => aliases.InvariantContains(x.Handler.Alias));


        private uSyncHandlerSetSettings GetSetSettings(string name)
        {
            return handlerSetSettingsAccessor.Get(name);
        }


        private HandlerConfigPair LoadHandlerConfig(ISyncHandler handler, uSyncHandlerSetSettings setSettings)
        {
            return new HandlerConfigPair
            {
                Handler = handler,
                Settings = setSettings.GetHandlerSettings(handler.Alias)
            };
        }

        public IEnumerable<HandlerConfigPair> GetValidHandlers(SyncHandlerOptions options = null)
        {
            if (options == null) options = new SyncHandlerOptions();

            var configs = new List<HandlerConfigPair>();

            var handlerSetSettings = GetSetSettings(options.Set);

            foreach (var handler in syncHandlers.Handlers.Where(x => x.Enabled))
            {
                if (handlerSetSettings.DisabledHandlers.InvariantContains(handler.Alias)) continue;

                // check its valid for the passed group and action. 
                if (handler != null && IsValidGroup(options.Group, handler))
                {
                    configs.Add(LoadHandlerConfig(handler, handlerSetSettings));
                }
                else
                {
                    // only log if we are doing the default 'everything' group 
                    // because weh nfoing groups we choose not to load things. 
                    if (string.IsNullOrWhiteSpace(options.Group))
                        logger.LogWarning("No Handler with {alias} has been loaded", handler.Alias);
                }

            }

            return configs.OrderBy(x => x.Handler.Priority);
        }

        private bool IsValidAction(HandlerActions requestedAction, string[] actions)
            => requestedAction == HandlerActions.None ||
                actions.InvariantContains("all") ||
                actions.InvariantContains(requestedAction.ToString());

        private bool IsValidGroup(string group, ISyncHandler handler)
        {
            // empty means all 
            if (string.IsNullOrWhiteSpace(group)) return true;

            // only handlers in the specified group
            if (handler is ISyncHandler extendedHandler)
                return extendedHandler.Group.InvariantEquals(group);

            return false;
        }
        #endregion
    }


}
