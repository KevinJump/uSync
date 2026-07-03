using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Linq;

using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.SyncHandlers;

/// <inheritdoc/>
public class SyncHandlerFactory : ISyncHandlerFactory
{
    private readonly SyncHandlerCollection _syncHandlers;
    private readonly ISyncConfigService _configService;
    private readonly ILogger<SyncHandlerFactory> _logger;

    /// <summary>
    ///  Create a new SyncHandlerFactory object
    /// </summary>
    public SyncHandlerFactory(
        ILogger<SyncHandlerFactory> logger,
        SyncHandlerCollection syncHandlers,
        ISyncConfigService configService)
    {
        _logger = logger;
        _syncHandlers = syncHandlers;
        _configService = configService;
    }

    /// <inheritdoc/>
    public string DefaultSet => this._configService.Settings.DefaultSet;

    #region All getters (regardless of set or config)

    /// <inheritdoc/>
    public IEnumerable<ISyncHandler> GetAll()
        => _syncHandlers.Handlers;

    /// <inheritdoc/>
    public ISyncHandler? GetHandler(string alias)
        => _syncHandlers.Handlers
            .FirstOrDefault(x => x.Alias.InvariantEquals(alias));

    /// <inheritdoc/>
    public IEnumerable<ISyncHandler> GetHandlers(params string[] aliases)
        => _syncHandlers.Where(x => aliases.InvariantContains(x.Alias));

    /// <inheritdoc/>
    public IEnumerable<string> GetGroups()
        => _syncHandlers.Handlers
            .Select(x => x.Group)
            .Distinct();

    #endregion

    #region Default Config Loaders - for when you know exactly what you want

    /// <inheritdoc/>>
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

    /// <summary>
    ///  not happy :( but this is a workaround for the fact that the ElementContainer handler is actually an Element handler,
    ///  but we need to be able to get it by its type name. ideally we would want to extend ISyncHandler to have multiple ItemTypes,
    ///  but that would be a breaking change, and we don't want to add that complexity to the interface. so for now, we just clean it up here.
    /// </summary>
    private string GetCleanItemType(string itemType)
        => itemType.Equals("ElementContainer") ? "Element" : itemType;

    /// <inheritdoc/>
    public HandlerConfigPair? GetValidHandler(string alias, SyncHandlerOptions? options = null)
         => GetValidHandlers(options)
             .FirstOrDefault(x => x.Handler.Alias.InvariantEquals(alias));

    /// <inheritdoc/>
    public HandlerConfigPair? GetValidHandlerByTypeName(string itemType, SyncHandlerOptions? options = null)
    {
        var cleanType = GetCleanItemType(itemType);
        return GetValidHandlers(options)
            .FirstOrDefault(x => cleanType.InvariantEquals(x.Handler.TypeName));
    }


    /// <inheritdoc/>
    public HandlerConfigPair? GetValidHandlerByEntityType(string entityType, SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .FirstOrDefault(x => x.Handler.EntityType.InvariantEquals(entityType) is true);

    /// <inheritdoc/>
    public HandlerConfigPair? GetValidHander<TObject>(SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .Where(x => x.Handler.ItemType == typeof(TObject).Name)
            .FirstOrDefault();

    /// <inheritdoc/>
    public IEnumerable<HandlerConfigPair> GetValidHandlersByEntityType(IEnumerable<string> entityTypes, SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .Where(x => entityTypes.InvariantContains(x.Handler.EntityType));


    /// <inheritdoc/>
    public IEnumerable<string> GetValidGroups(SyncHandlerOptions? options = null)
    {
        var handlers = GetValidHandlers(options);
        var groups = handlers
            .Select(x => x.GetConfigGroup())
            .ToList();

        groups.AddRange(handlers.Where(x => !string.IsNullOrWhiteSpace(x.Settings.Group))
            .Select(x => x.Settings.Group));

        return groups.Distinct();
    }

    /// <inheritdoc/>
    public IDictionary<string, string> GetValidHandlerGroupsAndIcons(SyncHandlerOptions? options = null)
    {
        var handlers = GetValidHandlers(options);

        return handlers.Select(x => new { group = x.GetConfigGroup(), icon = x.GetGroupIcon() })
            .DistinctBy(x => x.group)
            .ToDictionary(k => k.group, v => v.icon);
    }

    /// <inheritdoc/>
    public IEnumerable<HandlerConfigPair> GetValidHandlers(string[] aliases, SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .Where(x => aliases.InvariantContains(x.Handler.Alias));

    private static HandlerConfigPair LoadHandlerConfig(ISyncHandler handler, uSyncHandlerSetSettings setSettings)
    {
        return new HandlerConfigPair
        {
            Handler = handler,
            Settings = setSettings.GetHandlerSettings(handler.Alias)
        };
    }

    /// <inheritdoc/>
    public IEnumerable<HandlerConfigPair> GetValidHandlers(SyncHandlerOptions? options = null)
    {
        options ??= new SyncHandlerOptions();

        var configs = new List<HandlerConfigPair>();

        var handlerSetSettings = _configService.GetSetSettings(options.Set);

        foreach (var handler in _syncHandlers.Handlers.Where(x => options.IncludeDisabled || x.Enabled))
        {
            if (handler is null) continue;

            if (!options.IncludeDisabled && handlerSetSettings.DisabledHandlers.InvariantContains(handler.Alias))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace("Handler {handler} is in the disabled handler list", handler.Alias);

                continue;
            }

            var config = LoadHandlerConfig(handler, handlerSetSettings);

            // check its valid for the passed group and action. 
            if (IsValidHandler(config, options.Action, options.Group))
            {
                configs.Add(config);
            }
            else
            {
                // _logger.LogDebug("No Handler with {alias} has been loaded", handler.Alias);
                // only log if we are doing the default 'everything' group 
                // because when doing groups we choose not to load things. 
                // if (string.IsNullOrWhiteSpace(options.Group))
                //  _logger.LogWarning("No Handler with {alias} has been loaded", handler.Alias);
            }

        }

        return configs.OrderBy(x => x.Handler.Priority);
    }
    #endregion

    /// <summary>
    ///  is this config pair valid for the settings we have for it. 
    /// </summary>
    private static bool IsValidHandler(HandlerConfigPair handlerConfigPair, HandlerActions actions, string group)
        => handlerConfigPair.IsEnabled() && handlerConfigPair.IsValidAction(actions) && handlerConfigPair.IsValidGroup(group);
}
