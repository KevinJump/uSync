param ($suffix, $env='release', [switch]$push=$false)

dotnet pack ..\uSync.Core\uSync.Core.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync.Community.Contrib\uSync.Community.Contrib.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj -c $env -o .\$suffix --version-suffix $suffix /p:ContinuousIntegrationBuild=true 

.\nuget pack "..\uSync\uSync.nuspec" -version 9.0.0-$suffix -OutputDirectory .\$suffix
.\nuget pack "..\uSync.BackOffice.Assets\uSync.BackOffice.StaticAssets.nuspec" -version 9.0.0-$suffix -OutputDirectory .\$suffix

if ($push) {
    .\nuget.exe push ".\$suffix\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}