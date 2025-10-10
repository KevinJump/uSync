using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using Umbraco.Cms.Api.Management.OpenApi;

namespace uSync.Backoffice.Management.Api.Configuration;
public class ConfigSyncApiSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(
          "uSync",
          new OpenApiInfo
          {
              Title = "uSync Management Api",
              Version = "Latest",
              Description = "Api access uSync operations"
          });

        options.OperationFilter<uSyncClientOperationSecurityFilter>();

    }
}

public class uSyncClientOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
    protected override string ApiName => "uSync";
}
