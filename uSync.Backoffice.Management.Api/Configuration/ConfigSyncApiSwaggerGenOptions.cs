using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

using Umbraco.Cms.Api.Management.OpenApi;

namespace uSync.Backoffice.Management.Api.Configuration;
public class ConfigSyncApiSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(
          uSyncClient.Api.ApiName,
          new OpenApiInfo
          {
              Title = "uSync Management Api",
              Version = "Latest",
              Description = "Api access uSync operations"
          });

        options.OperationFilter<uSyncClientOperationSecurityFilter>();

        // uSyncAction.Exception is a System.Exception, left alone swashbuckle
        // walks TargetSite (MethodBase) and pulls the whole reflection graph
        // (Assembly, Module, Type, IntPtr...) into the spec. describe it as
        // a simple object with the bits a client might actually read.
        options.MapType<Exception>(() => new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["stackTrace"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["source"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["helpLink"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["hResult"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
            }
        });
    }
}

public class uSyncClientOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
    protected override string ApiName => uSyncClient.Api.ApiName;
}
