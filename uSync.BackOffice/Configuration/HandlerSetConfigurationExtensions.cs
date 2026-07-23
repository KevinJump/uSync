using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace uSync.BackOffice.Configuration;

/// <summary>
///  Service collection helpers for registering a uSync handler set from configuration.
/// </summary>
public static class HandlerSetConfigurationExtensions
{
    /// <summary>
    ///  Bind a handler set from configuration, and layer each named handler's own settings
    ///  over the top of the set's <see cref="uSyncHandlerSetSettings.HandlerDefaults"/>.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This is a drop-in replacement for <c>services.Configure&lt;uSyncHandlerSetSettings&gt;(setName, section)</c>.
    ///  </para>
    ///  <para>
    ///   After the normal bind, each handler that has its own block is re-bound on top of a clone of
    ///   the resolved <see cref="uSyncHandlerSetSettings.HandlerDefaults"/>. Because configuration binding
    ///   only writes the keys that are actually present, a handler inherits every default it does not
    ///   explicitly override - including the strongly typed properties (UseFlatStructure, GuidNames, etc.)
    ///   that can't be merged after binding (an unset boolean is indistinguishable from one set to its
    ///   default value once bound).
    ///  </para>
    ///  <para>
    ///   The additional <see cref="HandlerSettings.Settings"/> dictionary is also merged here, and is
    ///   merged again (idempotently) at resolution time in
    ///   <see cref="HandlerSetSettingsExtensions.GetHandlerSettings"/> so that keys added to
    ///   <see cref="uSyncHandlerSetSettings.HandlerDefaults"/> by <i>later</i> post-configure steps still cascade.
    ///  </para>
    /// </remarks>
    public static IServiceCollection ConfigureHandlerSet(this IServiceCollection services, string setName, IConfigurationSection section)
    {
        services.Configure<uSyncHandlerSetSettings>(setName, section);
        services.PostConfigure<uSyncHandlerSetSettings>(setName, options => MergeHandlerDefaults(options, section));
        return services;
    }

    /// <summary>
    ///  Re-bind every named handler in the set on top of a clone of the handler defaults.
    /// </summary>
    internal static void MergeHandlerDefaults(uSyncHandlerSetSettings options, IConfigurationSection section)
    {
        if (options.Handlers is null || options.Handlers.Count == 0)
            return;

        var handlersSection = section.GetSection("Handlers");

        foreach (var alias in options.Handlers.Keys.ToList())
        {
            // start from the resolved defaults, then bind the handler's own raw config over the
            // top - only the keys present in the handler block will override the defaults.
            var merged = options.HandlerDefaults.Clone();
            handlersSection.GetSection(alias).Bind(merged);
            options.Handlers[alias] = merged;
        }
    }
}
