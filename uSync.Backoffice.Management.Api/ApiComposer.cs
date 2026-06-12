using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

using uSync.Backoffice.Management.Api.Configuration;
using uSync.Backoffice.Management.Api.Services;
using uSync.BackOffice;
using uSync.Core.Extensions;

namespace uSync.Backoffice.Management.Api;

[ComposeAfter(typeof(uSyncBackOfficeComposer))]
public class ApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        if (builder.IsUmbracoBackOfficeEnabled() is false)
            return;

        builder.AddSyncOpenApi();
        builder.Services.AddSingleton<ISyncManagementCache, uSyncManagementCache>();
        builder.Services.AddSingleton<ISyncManagementService, uSyncManagementService>();
    }
}
