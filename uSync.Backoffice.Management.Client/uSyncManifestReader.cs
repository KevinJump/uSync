using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Semver;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Extensions;

namespace uSync.Backoffice.Management.Client;

public class uSyncManifestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, uSyncManifestReader>();
    }
}

internal sealed class uSyncManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var entrypoint = JsonObject.Parse(@"{""name"": ""usync.entrypoint"",
            ""alias"": ""uSync.EntryPoint"",
            ""type"": ""entryPoint"",
            ""js"": ""/App_Plugins/uSync/usync.js""}");

        List<PackageManifest> manifest = [
            new PackageManifest
            {
                Id = "uSync",
                Name = "uSync",
                AllowTelemetry = true,
                Version = GetuSyncVersion(),
                Extensions = [ entrypoint!],
                Importmap = new PackageManifestImportmap
                {
                    Imports = new Dictionary<string, string>
                    {
                        {  "@jumoo/uSync", "/App_Plugins/uSync/usync.js" },
                        {  "@jumoo/uSync/external/signalr", "/App_Plugins/uSync/usync.js" }
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
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.GetAssemblyFile().FullName);
            var productVersion = SemVersion.Parse(fileVersionInfo.ProductVersion ?? assembly.GetName()?.Version?.ToString(3) ?? "14.2.0");
            return productVersion.ToSemanticStringWithoutBuild();
        }
        catch
        {
            return assembly.GetName()?.Version?.ToString(3) ?? "14.0.0";
        }
    }
}
