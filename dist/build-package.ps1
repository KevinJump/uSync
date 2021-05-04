param ($suffix, $env='release', [switch]$push=$false)

dotnet pack ..\uSync.Core\uSync.Core.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync.Community.Contrib\uSync.Community.Contrib.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync.BackOffice.Assets\uSync.BackOffice.Assets.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync\uSync.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 

if ($push) {
    nuget push .\$suffix\*.nupkg -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}