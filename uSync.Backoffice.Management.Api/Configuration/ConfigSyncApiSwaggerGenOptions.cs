using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

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

        options.CustomOperationIds(e => $"{e.ActionDescriptor.RouteValues["action"]}");
    }
}
