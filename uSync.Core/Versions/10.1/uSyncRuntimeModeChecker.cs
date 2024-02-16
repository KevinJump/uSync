using Microsoft.Extensions.Configuration;

namespace uSync.Core.Versions;

/// <summary>
///  class to use some reflection to check if we are running in production
/// </summary>
public static class uSyncRuntimeModeChecker
{
    public static bool IsUmbracoRunningInProductionMode(this IConfiguration configuration)
    {
        // we could do reflection, but relection of static extensions methods !
        // there be dragons, so we are 'just' going to read the value from the 
        // config, and keep an eye on it should it ever move (that would be a breaking change!)

        var mode = configuration.GetValue<string>("Umbraco:Cms:Runtime:Mode", "BackofficeDevelopment");
        return mode.Equals("Production", StringComparison.InvariantCultureIgnoreCase);
    }
}
