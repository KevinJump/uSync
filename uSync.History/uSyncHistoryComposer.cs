using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Text.Json.Nodes;

using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Extensions;

using uSync.BackOffice;
using uSync.BackOffice.Extensions;
using uSync.Core.Extensions;
using uSync.History.Service;

namespace uSync.History
{
    public class uSyncHistoryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // don't load if the backoffice is not loaded as part of the project. 
            if (builder.IsUmbracoBackOfficeEnabled() is false)
                return;

            builder.Services.AddSingleton<ISyncHistoryService, SyncHistoryService>();

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
                SyncHistoryConstants.ApiName,
                new OpenApiInfo
                {
                    Title = $"{SyncHistoryConstants.DisplayName} API",
                    Version = "Latest",
                    Description = $"{SyncHistoryConstants.DisplayName} API methods"
                });

            options.OperationFilter<uSyncHistoryClientOperationSecurityFilter>();

        }
    }

    public class uSyncHistoryClientOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
    {
        protected override string ApiName => SyncHistoryConstants.ApiName;
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
            => controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(SyncHistoryConstants.ApiName) is true;

        public string Handle(ApiDescription apiDescription)
            => $"{apiDescription.ActionDescriptor.RouteValues["action"]}";
    }

    internal class uSyncHistoryManifestReader : IPackageManifestReader
    {
        public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        {
            var version = GetuSyncVersion();
            var script = $"{SyncHistoryConstants.PlugnPath}history.js?v={version}";

            List<PackageManifest> manifest = [
                new PackageManifest
            {
                Id = SyncHistoryConstants.AppName,
                Name = SyncHistoryConstants.DisplayName,
                AllowTelemetry = true,
                Version = GetuSyncVersion(),
                Extensions = [ new JsonObject {
                    ["name"] = $"{SyncHistoryConstants.AppName}.entrypoint",
                    ["alias"] = $"{SyncHistoryConstants.DisplayName} EntryPoint",
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
                return assembly.GetName()?.Version?.ToString(3) ?? "17.0.0";
            }
        }
    }
}
