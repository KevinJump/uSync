using Umbraco.Cms.Web.Common.Routing;

namespace uSync.Backoffice.Management.Api.Configuration;

public class uSyncVersionedRouteAttribute : BackOfficeRouteAttribute
{
    public uSyncVersionedRouteAttribute(string template)
        : base($"usync/api/v{{version:apiVersion}}/{template.TrimStart('/')}")
    { }
}
