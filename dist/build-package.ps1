param ($version = '9.0.0', $suffix, $env='release', [switch]$push=$false)

$fullVersion = -join($version, '-', $suffix)
$outFolder = ".\$fullVersion"

dotnet pack ..\uSync.Core\uSync.Core.csproj -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullVersion 
dotnet pack ..\uSync.Community.Contrib\uSync.Community.Contrib.csproj -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullversion  
dotnet pack ..\uSync.Community.DataTypeSerializers\uSync.Community.DataTypeSerializers.csproj -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullversion  
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullVersion 

dotnet pack ..\uSync.AutoTemplates\uSync.AutoTemplates.csproj -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullVersion 

.\nuget pack "..\uSync\uSync.nuspec" -version $fullVersion -OutputDirectory $outFolder
.\nuget pack "..\uSync.BackOffice.Assets\uSync.BackOffice.StaticAssets.nuspec" -version $fullVersion -OutputDirectory $outFolder


XCOPY "$outFolder\*.nupkg" "C:\Source\localgit" /Q /Y 

if ($push) {
    .\nuget.exe push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}