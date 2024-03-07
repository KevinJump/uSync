using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


using Umbraco.Extensions;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers;

/// <summary>
///  Factory method for accessing the handlers and their configuration
/// </summary>
public class SyncHandlerFactory
{
    private SyncHandlerCollection _syncHandlers;
    private uSyncSettings _settings;
    private ILogger<SyncHandlerFactory> _logger;

    private IOptionsMonitor<uSyncHandlerSetSettings> _handlerSetSettingsAccessor;

    /// <summary>
    ///  Create a new SyncHandlerFactory object
    /// </summary>
    public SyncHandlerFactory(
        ILogger<SyncHandlerFactory> logger,
        SyncHandlerCollection syncHandlers,
        IOptionsMonitor<uSyncHandlerSetSettings> handlerSetSettingsAccessor,
        IOptionsMonitor<uSyncSettings> options)
    {
        _handlerSetSettingsAccessor = handlerSetSettingsAccessor;
        _logger = logger;
        _syncHandlers = syncHandlers;
        _settings = options.CurrentValue;
    }

    /// <summary>
    ///  Name of the default handler set
    /// </summary>
    public string DefaultSet => this._settings.DefaultSet;

    #region All getters (regardless of set or config)

    /// <summary>
    ///  Get all handlers 
    /// </summary>
    public IEnumerable<ISyncHandler> GetAll()
        => _syncHandlers.Handlers;

    /// <summary>
    ///  Get a handler by alias 
    /// </summary>
    public ISyncHandler? GetHandler(string alias)
        => _syncHandlers.Handlers
            .FirstOrDefault(x => x.Alias.InvariantEquals(alias));

    /// <summary>
    ///  Get all handlers that match the list of names in the aliases array 
    /// </summary>
    public IEnumerable<ISyncHandler> GetHandlers(params string[] aliases)
        => _syncHandlers.Where(x => aliases.InvariantContains(x.Alias));

    /// <summary>
    ///  returns the handler groups (settings, content, users, etc) that stuff can be grouped into
    /// </summary>
    public IEnumerable<string> GetGroups()
        => _syncHandlers.Handlers
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

    /// <summary>
    ///  Get a valid handler (based on config) by alias and options
    /// </summary>
    public HandlerConfigPair? GetValidHandler(string alias, SyncHandlerOptions? options = null)
         => GetValidHandlers(options)
             .FirstOrDefault(x => x.Handler.Alias.InvariantEquals(alias));

    /// <summary>
    ///  Get a valid handler (based on config) by ItemType and options
    /// </summary>
    public HandlerConfigPair? GetValidHandlerByTypeName(string itemType, SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .Where(x => itemType.InvariantEquals(x.Handler.TypeName))
            .FirstOrDefault();

    /// <summary>
    ///  Get a valid handler (based on config) by Umbraco Entity Type and options
    /// </summary>
    public HandlerConfigPair? GetValidHandlerByEntityType(string entityType, SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .FirstOrDefault(x => x.Handler.EntityType.InvariantEquals(entityType) is true);

    /// <summary>
    ///  Get a valid handler (based on config) by options
    /// </summary>
    public HandlerConfigPair? GetValidHander<TObject>(SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .Where(x => x.Handler.ItemType == typeof(TObject).Name)
            .FirstOrDefault();


    /// <summary>
    ///  Get a all valid handlers (based on config) that can handle a given entityType
    /// </summary>
    public IEnumerable<HandlerConfigPair> GetValidHandlersByEntityType(IEnumerable<string> entityTypes, SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .Where(x => entityTypes.InvariantContains(x.Handler.EntityType));


    /// <summary>
    ///  Get the valid (by config) handler groups avalible to this setup
    /// </summary>
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

    /// <summary>
    ///  get the handler groups and their icons
    /// </summary>
    /// <remarks>
    ///  if we don't have a defined icon for a group, the icon from the first handler in the group
    ///  will be used. 
    /// </remarks>
    public IDictionary<string, string> GetValidHandlerGroupsAndIcons(SyncHandlerOptions? options = null)
    {
        var handlers = GetValidHandlers(options);

        return handlers.Select(x => new { group = x.GetConfigGroup(), icon = x.GetGroupIcon() })
            .DistinctBy(x => x.group)
            .ToDictionary(k => k.group, v => v.icon);
    }

    /// <summary>
    ///  get a collection of valid handlers that match the list of aliases 
    /// </summary>
    public IEnumerable<HandlerConfigPair> GetValidHandlers(string[] aliases, SyncHandlerOptions? options = null)
        => GetValidHandlers(options)
            .Where(x => aliases.InvariantContains(x.Handler.Alias));


    private uSyncHandlerSetSettings GetSetSettings(string name)
    {
        return _handlerSetSettingsAccessor.Get(name);
    }


    private HandlerConfigPair LoadHandlerConfig(ISyncHandler handler, uSyncHandlerSetSettings setSettings)
    {
        return new HandlerConfigPair
        {
            Handler = handler,
            Settings = setSettings.GetHandlerSettings(handler.Alias)
        };
    }

    /// <summary>
    /// Get all valid (by configuration) handlers that fufill the criteria set out in the passed SyncHandlerOptions 
    /// </summary>
    public IEnumerable<HandlerConfigPair> GetValidHandlers(SyncHandlerOptions? options = null)
    {
        if (options == null) options = new SyncHandlerOptions();

        var configs = new List<HandlerConfigPair>();

        var handlerSetSettings = GetSetSettings(options.Set);

        foreach (var handler in _syncHandlers.Handlers.Where(x => options.IncludeDisabled || x.Enabled))
        {
            if (handler is null) continue;

            if (!options.IncludeDisabled && handlerSetSettings.DisabledHandlers.InvariantContains(handler.Alias))
            {
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
                _logger.LogDebug("No Handler with {alias} has been loaded", handler.Alias);
                // only log if we are doing the default 'everything' group 
                // because when doing groups we choose not to load things. 
                if (string.IsNullOrWhiteSpace(options.Group))
                    _logger.LogWarning("No Handler with {alias} has been loaded", handler.Alias);
            }

        }

        return configs.OrderBy(x => x.Handler.Priority);
    }
    #endregion

    /// <summary>
    ///  is this config pair valid for the settings we have for it. 
    /// </summary>
    private bool IsValidHandler(HandlerConfigPair handlerConfigPair, HandlerActions actions, string group)
        => handlerConfigPair.IsEnabled() && handlerConfigPair.IsValidAction(actions) && handlerConfigPair.IsValidGroup(group);
}
