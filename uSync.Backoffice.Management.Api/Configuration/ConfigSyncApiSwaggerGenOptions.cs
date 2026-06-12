using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.Backoffice.Management.Api.Configuration;

public static class SyncOpenApiExtensions
{
    public static IUmbracoBuilder AddSyncOpenApi(this IUmbracoBuilder builder)
        => builder.AddBackOfficeOpenApiDocument(
            uSyncClient.Api.ApiName,
            document => document
                .WithTitle("uSync Management Api")
                .WithBackOfficeAuthentication()
                .ConfigureOpenApiOptions(options =>
                {
                    options.AddDocumentTransformer((doc, _, _) =>
                    {
                        doc.Info.Version = "1.0";
                        return Task.CompletedTask;
                    });
                })
            );
}
