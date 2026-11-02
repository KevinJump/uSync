using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

using uSync.Backoffice.Management.Api.Configuration;
using uSync.Backoffice.Management.Api.Preview;
using uSync.Backoffice.Management.Api.Services;
using uSync.BackOffice;
using uSync.Core.Extensions;

namespace uSync.Backoffice.Management.Api;

[ComposeAfter(typeof(uSyncBackOfficeComposer))]
public class ApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // the preview mappers in this assembly are in the (uSync.BackOffice) mapper
        // collection either way, so the helper they use always needs to be there.
        builder.Services.AddSingleton<ISyncPreviewMappingHelper, SyncPreviewMappingHelper>();

        if (builder.IsUmbracoBackOfficeEnabled() is false)
            return;

        builder.Services.AddSingleton<IOperationIdHandler, uSyncCustomOperationHandler>();

        builder.Services.ConfigureOptions<ConfigSyncApiSwaggerGenOptions>();
        builder.Services.AddSingleton<ISyncManagementCache, uSyncManagementCache>();
        builder.Services.AddSingleton<ISyncManagementService, uSyncManagementService>();
    }
}
