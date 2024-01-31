using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

using uSync.Backoffice.Management.Api.Configuration;

namespace uSync.Backoffice.Management.Api;
public class ApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.ConfigureOptions<ConfigSyncApiSwaggerGenOptions>();
    }
}
