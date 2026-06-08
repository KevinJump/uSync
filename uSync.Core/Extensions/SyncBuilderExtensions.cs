using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.Core.Extensions;

public static class SyncBuilderExtensions
{
    public static bool IsUmbracoBackOfficeEnabled(this IUmbracoBuilder builder)
        => builder.Services.Any(s => s.ServiceType == typeof(IBackOfficeEnabledMarker));
}
