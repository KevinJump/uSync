using Microsoft.Extensions.DependencyInjection;

using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Extensions;
using uSync.Core.Extensions;

namespace uSync.Backoffice.Management.Client;

[ComposeAfter(typeof(BackOffice.uSyncBackOfficeComposer))]
public class uSyncManifestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        if (builder.IsUmbracoBackOfficeEnabled() is false) return;

        // only load this when the backoffice is enabled.
        builder.Services.AddSingleton<IPackageManifestReader, uSyncManifestReader>();
        builder.Services.AddSingleton<IPackageManifestReader, SyncSectionManifestReader>();
    }
}

internal sealed class SyncSectionManifestReader : IPackageManifestReader
{
    private readonly ISyncConfigService _configService;

    public SyncSectionManifestReader(ISyncConfigService configService)
    {
        _configService = configService;
    }

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        if (_configService.Settings.MoveToSection is false)
            return Task.FromResult(Enumerable.Empty<PackageManifest>());

        List<PackageManifest> manifest = [
            new PackageManifest
            {
                Id = "uSync.Section",
                Name = "uSync Section",
                AllowTelemetry = false,
                Version = typeof(SyncSectionManifestReader).Assembly.GetName()?.Version?.ToString(3) ?? "17.0.0",
                Extensions = [ new JsonObject {
                    ["type"] = "section",
                    ["name"] = "uSync Section",
                    ["alias"] = "usync.section",
                    ["weight"] = 350,
                    ["meta"] = new JsonObject {
                        ["label"] = "#uSync_section",
                        ["pathname"] = "sync"
                    }
                }]
            }
        ];
        return Task.FromResult(manifest.AsEnumerable());
    }
}

internal sealed class uSyncManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var version = GetuSyncVersion();
        var script = $"/App_Plugins/uSync/uSync.js?v={version}";

        List<PackageManifest> manifest = [
            new PackageManifest
            {
                Id = "uSync",
                Name = "uSync",
                AllowTelemetry = true,
                Version = GetuSyncVersion(),
                Extensions = [ new JsonObject {
                    ["name"] = "usync.entrypoint",
                    ["alias"] = "uSync EntryPoint",
                    ["type"] = "backofficeEntryPoint",
                    ["js"] = script
                }],
                Importmap = new PackageManifestImportmap
                {
                    Imports = new Dictionary<string, string>
                    {
                        {  "@jumoo/uSync", script },
                        {  "@jumoo/uSync/external/signalr", script },
                        {  "@jumoo/usync", script },
                        {  "@jumoo/usync/external/signalr", script }
                    }
                }
            }
        ];

        return Task.FromResult(manifest.AsEnumerable());
    }

    private string GetuSyncVersion()
    {
        var assembly = typeof(uSyncManifestReader).Assembly;
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
