using System;

using Umbraco.Cms.Core.Scoping;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.Extensions;
internal static class ScopeExtensions
{
    public static IDisposable SuppressScopeByConfig(this ICoreScope scope, uSyncConfigService configService)
        => configService.Settings.DisableNotificationSuppression
            ? new DummyDisposable()
            : scope.Notifications.Suppress();
}

/// <summary>
///  a dummy disposable class, so we can use it when we don't suppress notifications.
/// </summary>
internal class DummyDisposable : IDisposable
{
    public void Dispose()
    {
        // nothing to dispose. 
    }
}
