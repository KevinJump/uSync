param ($version = '9.0.0', $suffix, $env='release', [switch]$push=$false)

$fullVersion = -join($version, '-', $suffix)
$outFolder = ".\$fullVersion"

dotnet pack ..\uSync.Core\uSync.Core.csproj -c $env -o $outFolder --version-suffix $suffix /p:ContinuousIntegrationBuild=true,version=$version 
dotnet pack ..\uSync.Community.Contrib\uSync.Community.Contrib.csproj -c $env -o $outFolder --version-suffix $suffix /p:ContinuousIntegrationBuild=true,version=$version  
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj -c $env -o $outFolder --version-suffix $suffix /p:ContinuousIntegrationBuild=true,version=$version 

.\nuget pack "..\uSync\uSync.nuspec" -version 9.0.0-$suffix -OutputDirectory $outFolder
.\nuget pack "..\uSync.BackOffice.Assets\uSync.BackOffice.StaticAssets.nuspec" -version $fullVersion -OutputDirectory $outFolder

if ($push) {
    .\nuget.exe push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}