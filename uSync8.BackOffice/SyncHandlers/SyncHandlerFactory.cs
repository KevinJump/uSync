using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;

using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    public class SyncHandlerFactory
    {
        private SyncHandlerCollection syncHandlers;
        private uSyncConfig config;

        public SyncHandlerFactory(SyncHandlerCollection syncHandlers, uSyncConfig config)
        {
            this.syncHandlers = syncHandlers;
            this.config = config;
        }

        /// <summary>
        ///  default set so others don't need to look it up.
        /// </summary>
        public string DefaultSet => config.Settings.DefaultSet;

        #region All getters (regardless of set or config)

        public IEnumerable<ISyncHandler> GetAll()
            => syncHandlers.Handlers;

        public IEnumerable<ISyncExtendedHandler> GetExtended()
            => syncHandlers.ExtendedHandlers;

        public ISyncExtendedHandler GetHandler(string alias)
            => syncHandlers.ExtendedHandlers
                .FirstOrDefault(x => x.Alias.InvariantEquals(alias));

        [Obsolete("You should avoid asking for a handler just by type, ask for valid based on set too")]
        public ISyncExtendedHandler GetHandler(Udi udi)
            => GetHandlerByEntityType(udi.EntityType);

        public IEnumerable<ISyncHandler> GetHandlers(params string[] aliases)
            => syncHandlers.Where(x => aliases.InvariantContains(x.Alias));

        [Obsolete("You should avoid asking for a handler just by type, ask for valid based on set too")]
        public ISyncExtendedHandler GetHandlerByEntityType(string entityType)
            => syncHandlers.ExtendedHandlers
                .FirstOrDefault(x => x.EntityType == entityType);

        [Obsolete("You should avoid asking for a handler just by type, ask for valid based on set too")]
        public ISyncExtendedHandler GetHandlerByTypeName(string typeName)
            => syncHandlers.ExtendedHandlers
                .FirstOrDefault(x => x.TypeName == typeName);

        /// <summary>
        ///  returns the handler groups (settings, content, users, etc) that stuff can be grouped into
        /// </summary>
        public IEnumerable<string> GetGroups()
            => syncHandlers.ExtendedHandlers
                .Select(x => x.Group)
                .Distinct();

        /// <summary>
        ///  returns the list of handler sets in the config 
        /// </summary>
        public IEnumerable<string> GetSets()
            => config.Settings.HandlerSets.Select(x => x.Name);

        #endregion

        #region Default Config Loaders - for when you know exactly what you want

        /// <summary>
        ///  Get Default Handlers based on Alias
        /// </summary>
        /// <param name="aliases">aliases of handlers you want </param>
        /// <returns>Handler/Config Pair with default config loaded</returns>
        public IEnumerable<ExtendedHandlerConfigPair> GetDefaultHandlers(IEnumerable<string> aliases)
            => GetExtended()
                    .Where(x => aliases.InvariantContains(x.Alias))
                    .Select(x => new ExtendedHandlerConfigPair()
                    {
                        Handler = x,
                        Settings = x.DefaultConfig
                    });

        #endregion


        #region Valid Loaders (need set, group, action)

        public ExtendedHandlerConfigPair GetValidHandler(string alias, SyncHandlerOptions options = null)
             => GetValidHandlers(options)
                 .FirstOrDefault(x => x.Handler.Alias.InvariantEquals(alias));

        public ExtendedHandlerConfigPair GetValidHandlerByTypeName(string typeName, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => typeName.InvariantEquals(x.Handler.TypeName))
                .FirstOrDefault();

        public ExtendedHandlerConfigPair GetValidHandlerByEntityType(string entityType, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => x.Handler.EntityType.InvariantEquals(entityType))
                .FirstOrDefault();


        public IEnumerable<ExtendedHandlerConfigPair> GetValidHandlersByEntityType(IEnumerable<string> entityTypes, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => entityTypes.InvariantContains(x.Handler.EntityType));


        public IEnumerable<string> GetValidGroups(SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Select(x => x.Handler.Group)
                .Distinct();

        public IEnumerable<ExtendedHandlerConfigPair> GetValidHandlers(string[] aliases, SyncHandlerOptions options = null)
            => GetValidHandlers(options)
                .Where(x => aliases.InvariantContains(x.Handler.Alias));

        public IEnumerable<ExtendedHandlerConfigPair> GetValidHandlers(SyncHandlerOptions options = null)
        {
            if (options == null) options = new SyncHandlerOptions(this.DefaultSet);
            EnsureHandlerSet(options);

            var set = config.Settings.HandlerSets
                .Where(x => x.Name.InvariantEquals(options.Set))
                .FirstOrDefault();

            if (set == null) return Enumerable.Empty<ExtendedHandlerConfigPair>();

            var configs = new List<ExtendedHandlerConfigPair>();

            foreach (var settings in set.Handlers.Where(x => x.Enabled))
            {
                // Get the handler 
                var handler = syncHandlers.ExtendedHandlers
                    .Where(x => x.Alias.InvariantEquals(settings.Alias))
                    .FirstOrDefault();

                // check its valid for the passed group and action. 
                if (handler != null
                    && IsValidGroup(options.Group, handler)
                    && IsValidAction(options.Action, settings.Actions))
                {
                    configs.Add(new ExtendedHandlerConfigPair()
                    {
                        Handler = handler,
                        Settings = settings
                    });
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

            if (handler is ISyncExtendedHandler extendedHandler)
                return extendedHandler.Group.InvariantEquals(group);

            return false;
        }

        private void EnsureHandlerSet(SyncHandlerOptions handlerOptions)
        {
            if (string.IsNullOrWhiteSpace(handlerOptions.Set))
                handlerOptions.Set = this.DefaultSet;
        }

        #endregion
    }


}
