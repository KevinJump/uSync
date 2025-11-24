using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Extensions;
using uSync.BackOffice;
using uSync.BackOffice.Extensions;

namespace uSync.History
{
    public class uSyncHistoryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationAsyncHandler<uSyncImportCompletedNotification, uSyncHistoryNotificationHandler>();
            builder.AddNotificationAsyncHandler<uSyncExportCompletedNotification, uSyncHistoryNotificationHandler>();
            builder.Services.AddSingleton<IOperationIdHandler, MaintenanceModeCustomOperationHandler>();
            builder.Services.ConfigureOptions<ConfigureSwaggerGenOptions>();
            builder.Services.AddSingleton<IPackageManifestReader, uSyncHistoryManifestReader>();
        }
    }

    internal class ConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            options.SwaggerDoc(
                "uSync.History",
                new OpenApiInfo
                {
                    Title = "uSync History API",
                    Version = "Latest",
                    Description = "uSync History API methods"
                });

        }
    }

    public class MaintenanceModeCustomOperationHandler : IOperationIdHandler
    {
        public bool CanHandle(ApiDescription apiDescription)
        {
            if (apiDescription.ActionDescriptor is not
                ControllerActionDescriptor controllerActionDescriptor)
                return false;

            return CanHandle(apiDescription, controllerActionDescriptor);
        }

        public bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
            => controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith("uSync.History") is true;

        public string Handle(ApiDescription apiDescription)
            => $"{apiDescription.ActionDescriptor.RouteValues["action"]}";
    }

    internal class uSyncHistoryManifestReader : IPackageManifestReader
    {
        public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        {
            var version = GetuSyncVersion();
            var script = $"/App_Plugins/uSync.History/history.js?v={version}";

            List<PackageManifest> manifest = [
                new PackageManifest
            {
                Id = "uSync.History",
                Name = "uSync History",
                AllowTelemetry = true,
                Version = GetuSyncVersion(),
                Extensions = [ new JsonObject {
                    ["name"] = "usync.history.entrypoint",
                    ["alias"] = "uSync History EntryPoint",
                    ["type"] = "backofficeEntryPoint",
                    ["js"] = script
                }],
            }
            ];

            return Task.FromResult(manifest.AsEnumerable());
        }

        private string GetuSyncVersion()
        {
            var assembly = typeof(uSyncHistoryManifestReader).Assembly;
            try
            {
                return assembly.GetAssemblyProductVersion().ToSemanticStringWithoutBuild();
            }
            catch
            {
                return assembly.GetName()?.Version?.ToString(3) ?? "15.0.0";
            }
        }
    }
}
