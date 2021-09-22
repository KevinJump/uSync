<#
SYNOPSIS
    Buils the optionally pushes the uSync packages.
#>
param (

    [Parameter(Mandatory)]
    [string]
    [Alias("v")]  $version, #version to build

    [Parameter()]
    [string]
    $suffix, # optional suffix to append to version (for pre-releases)

    [Parameter()]
    [string]
    $env = 'release', #build environment to use when packing

    [Parameter()]
    [switch]
    $push=$false #push to devops nightly feed
)

if ($version.IndexOf('-') -ne -1) {
    Write-Host "Version shouldn't contain a - (remember version and suffix are seperate)"
    exit
}

$fullVersion = $version;

if (![string]::IsNullOrWhiteSpace($suffix)) {
   $fullVersion = -join($version, '-', $suffix)
}

$majorFolder = $version.Substring(0, $version.LastIndexOf('.'))

$outFolder = ".\$majorFolder\$version\$fullVersion"
if (![string]::IsNullOrWhiteSpace($suffix)) {
    $suffixFolder = $suffix.substring(0, $suffix.indexOf('.'))
    $outFolder = ".\$majorFolder\$version\$version-$suffixFolder\$fullVersion"
}

"----------------------------------"
Write-Host "Version  :" $fullVersion
Write-Host "Config   :" $env
Write-Host "Folder   :" $outFolder
"----------------------------------"; ""

dotnet restore ..

""; "##### Packaging"; "----------------------------------" ; ""

dotnet pack ..\uSync.Core\uSync.Core.csproj --no-restore -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullVersion 
dotnet pack ..\uSync.Community.Contrib\uSync.Community.Contrib.csproj --no-restore -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullversion  
dotnet pack ..\uSync.Community.DataTypeSerializers\uSync.Community.DataTypeSerializers.csproj --no-restore -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullversion  
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj --no-restore -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullVersion 

dotnet pack ..\uSync.AutoTemplates\uSync.AutoTemplates.csproj --no-restore -c $env -o $outFolder /p:ContinuousIntegrationBuild=true,version=$fullVersion 

.\nuget pack "..\uSync\uSync.nuspec" -version $fullVersion -OutputDirectory $outFolder
.\nuget pack "..\uSync.BackOffice.Assets\uSync.BackOffice.StaticAssets.nuspec" -version $fullVersion -OutputDirectory $outFolder

""; "##### Copying to LocalGit folder"; "----------------------------------" ; ""
XCOPY "$outFolder\*.nupkg" "C:\Source\localgit" /Q /Y 

if ($push) {
    ""; "##### Pushing to our nighly package feed"; "----------------------------------" ; ""
    .\nuget.exe push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}

Write-Host "uSync Packaged : $fullVersion"