using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.Backoffice.Management.Api;
public class ApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.ConfigureSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "uSync",
                new OpenApiInfo
                {
                    Title = "uSync Api",
                    Version = "Latest",
                    Description = "Api to access uSync operations"
                });
        });
    }
}
