using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public IEnumerable<ISyncHandler> GetAll(bool extended)
        {
            if (extended) return syncHandlers.ExtendedHandlers;
            return syncHandlers.Handlers;
        }

        public ISyncExtendedHandler GetHandler(string alias)
            => syncHandlers.ExtendedHandlers
                .FirstOrDefault(x => x.Alias.InvariantEquals(alias));

        public ISyncExtendedHandler GetHandler(Udi udi)
            => GetHandlerByEntityType(udi.EntityType);

        public IEnumerable<ISyncHandler> GetHandlers(params string[] aliases)
            => syncHandlers.Where(x => aliases.InvariantContains(x.Alias));

        public ISyncExtendedHandler GetHandlerByEntityType(string entityType)
            => syncHandlers.ExtendedHandlers
                .FirstOrDefault(x => x.EntityType == entityType);

        public ISyncExtendedHandler GetHandlerByTypeName(string typeName)
            => syncHandlers.ExtendedHandlers
                .FirstOrDefault(x => x.TypeName == typeName);

        public IEnumerable<string> GetGroups()
            => syncHandlers.ExtendedHandlers
                .Select(x => x.Group)
                .Distinct();

        #endregion

        #region Valid Loaders (need set, group, action)
        public HandlerConfigPair GetValidHandler(string alias)
            => GetValidHandler(alias, uSync.Handlers.DefaultSet);

        public HandlerConfigPair GetValidHandler(string alias, string setName)
            => GetValidHandler(alias, setName, string.Empty);

        public HandlerConfigPair GetValidHandler(string alias, string setName, string group)
            => GetValidHandler(alias, setName, group, string.Empty);

        public HandlerConfigPair GetValidHandler(string alias, string setName, string group, string action)
             => GetValidHandlers(setName, group, action)
                 .FirstOrDefault(x => x.Handler.Alias.InvariantEquals(alias));

        public IEnumerable<HandlerConfigPair> GetValidHandlers(string[] aliases, string setName, string group, string action)
            => GetValidHandlers(setName, group, action)
                .Where(x => aliases.InvariantContains(x.Handler.Alias));

        public IEnumerable<HandlerConfigPair> GetValidHandlersByEntityType(IEnumerable<string> entityTypes, string setName, string group, string action)
            => GetValidHandlers(setName, action, group)
                .Where(x => x is ISyncExtendedHandler 
                    && entityTypes.InvariantContains(((ISyncExtendedHandler)x.Handler).EntityType));

        public IEnumerable<string> GetValidGroups(string setName)
            => GetValidGroups(setName, string.Empty);

        public IEnumerable<string> GetValidGroups(string action, string setName)
            => GetValidHandlers(setName, string.Empty, action)
                .Where(x => x.Handler is ISyncExtendedHandler)
                .Select(x => ((ISyncExtendedHandler)x.Handler).Group)
                .Distinct();

        public IEnumerable<HandlerConfigPair> GetValidHandlers(string setName, string group)
            => GetValidHandlers(setName, group);

        public IEnumerable<HandlerConfigPair> GetValidHandlers(string setName, string group, string action)
        {
            if (string.IsNullOrWhiteSpace(setName))
                setName = this.DefaultSet;

            var set = config.Settings.HandlerSets
                .Where(x => x.Name.InvariantEquals(setName))
                .FirstOrDefault();

            if (set == null) return Enumerable.Empty<HandlerConfigPair>();

            var configs = new List<HandlerConfigPair>();

            foreach (var settings in set.Handlers)
            {
                var handler = syncHandlers.Handlers
                    .Where(x => x.Alias.InvariantEquals(settings.Alias))
                    .FirstOrDefault();

                if (handler != null)
                {
                    // is this filtered by group ?
                    if (string.IsNullOrWhiteSpace(group) || HandlerInGroup(handler, group))
                    {

                        // is this filtered by action 
                        if (string.IsNullOrWhiteSpace(action) || IsValidAction(settings.Actions, action))
                        {
                            configs.Add(new HandlerConfigPair()
                            {
                                Handler = handler,
                                Settings = settings
                            });

                        }

                    }
                }

            }

            return configs.OrderBy(x => x.Handler.Priority);
        }

        private bool IsValidAction(string[] actions, string requestedAction)
            => actions.InvariantContains("all") || actions.InvariantContains(requestedAction);

        private bool HandlerInGroup(ISyncHandler handler, string group)
        {
            if (handler is ISyncExtendedHandler extendedHandler)
                return extendedHandler.Group.InvariantEquals(group);

            return false;
        }

        #endregion
    }

}
