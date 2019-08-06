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
        ///  handlers that impliment the ISyncHandler2 interface, can be used for other things.
        /// </summary>
        private List<ISyncSingleItemHandler> handlerTwos;

        public SyncHandlerCollection(IEnumerable<ISyncHandler> items)
            : base(items)
        {
            handlerTwos = items
                .Where(x => x is ISyncSingleItemHandler)
                .Select(x => x as ISyncSingleItemHandler)
                .ToList();
        }

        public ISyncSingleItemHandler GetHandlerFromUdi(Udi udi)
        {
            return GetHandlerByEntityType(udi.EntityType);
        }

        public ISyncSingleItemHandler GetHandlerByEntityType(string entityType)
        {
            return handlerTwos.FirstOrDefault(x => x.EntityType == entityType);
        }

        public ISyncSingleItemHandler GetHandlerByTypeName(string typeName)
        {
            return handlerTwos.FirstOrDefault(x => x.TypeName == typeName);
        }

        public IEnumerable<HandlerConfigPair> GetHandlersByType(IEnumerable<string> entityTypes, string actionName, uSyncSettings settings)
        {
            var handlers = handlerTwos.Where(x => entityTypes.InvariantContains(x.EntityType));
            return GetConfiguredHandlers(handlers, actionName, settings, true);
        }

        public IEnumerable<HandlerConfigPair> GetValidHandlers(string actionName, uSyncSettings settings)
        {
            return GetConfiguredHandlers(this, actionName, settings, false);
        }

        public IEnumerable<HandlerConfigPair> GetValidHandlers(string actionName, string group, uSyncSettings settings)
        {
            var handlers = GetValidHandlers(actionName, settings);

            if (string.IsNullOrWhiteSpace(group)) return handlers;

            var groupedHandlers = new List<HandlerConfigPair>();

            foreach (var pair in handlers)
            {
                if (pair.Handler is ISyncSingleItemHandler groupedHandler)
                {
                    if (groupedHandler.Group.InvariantEquals(group))
                    {
                        groupedHandlers.Add(pair);
                    }
                }
                else if (group == uSyncBackOfficeConstants.Groups.Settings)
                {
                    groupedHandlers.Add(pair);
                }

            }

            return groupedHandlers;
        }

        public IEnumerable<string> GetGroups()
        {
            return handlerTwos.Select(x => x.Group).Distinct().ToList();
        }

        public IEnumerable<string> GetValidGroups(uSyncSettings settings)
        {
            var handlers = GetValidHandlers(HandlerActionNames.All, settings)
                .Where(x => x.Handler is ISyncSingleItemHandler)
                .Select(x => x.Handler as ISyncSingleItemHandler);

            return handlers.Select(x => x.Group).Distinct().ToList();
        }

        public IEnumerable<ISyncSingleItemHandler> GetHandlersByGroup(string groupName) {
            return handlerTwos.Where(x => x.Group.InvariantEquals(groupName));
        }

        public IEnumerable<HandlerConfigPair> GetConfiguredHandlers(IEnumerable<ISyncHandler> handlers, string actionName, uSyncSettings settings, bool includeDisabled)
        {
            var configPairs = new List<HandlerConfigPair>();

            foreach(var handler in handlers)
            {
                var config = settings.Handlers.FirstOrDefault(x => x.Alias.InvariantEquals(handler.Alias));
                if (config == null)
                {
                    config = new HandlerSettings(handler.Alias, settings.EnableMissingHandlers)
                    {
                        GuidNames = new OverriddenValue<bool>(settings.UseGuidNames, false),
                        UseFlatStructure = new OverriddenValue<bool>(settings.UseFlatStructure, false)
                    };
                }

                if (config != null && (includeDisabled || config.Enabled))
                {
                    configPairs.Add(new HandlerConfigPair() { Handler = handler, Settings = config });
                }
            }

            return configPairs.OrderBy(x => x.Handler.Priority);
        }
    }

    public class HandlerConfigPair
    {
        public ISyncHandler Handler { get; set; }
        public HandlerSettings Settings { get; set; }
    }
}
