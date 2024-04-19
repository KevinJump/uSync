using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

using uSync.Backoffice.Management.Api.Configuration;
using uSync.Backoffice.Management.Api.Services;
using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api;

[ComposeAfter(typeof(uSyncBackOfficeComposer))]
public class ApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IOperationIdHandler, uSyncCustomOperationHandler>();

        builder.Services.ConfigureOptions<ConfigSyncApiSwaggerGenOptions>();
        builder.Services.AddSingleton<ISyncManagementCache, uSyncManagementCache>();
        builder.Services.AddSingleton<ISyncManagementService, uSyncManagementService>();
    }
}
