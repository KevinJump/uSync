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
    $push=$false, #push to devops nightly feed

    [Parameter()]
    [switch]
    $skipClient=$false #do not do the client bit
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
    $suffixFolder = $suffix;
    if ($suffix.IndexOf('.') -ne -1) {
        $suffixFolder = $suffix.substring(0, $suffix.indexOf('.'))
    }
    $outFolder = ".\$majorFolder\$version\$version-$suffixFolder\$fullVersion"
}

# $buildParams = "ContinuousIntegrationBuild=true,version=$fullVersion"

"----------------------------------"
Write-Host "Version  :" $fullVersion
Write-Host "Config   :" $env
Write-Host "Folder   :" $outFolder
"----------------------------------"; ""

"Stamp version in umbraco-package.json"
$umbracoPackagePath = '../uSync.BackOffice.Management.Client/usync-assets/public/umbraco-package.json'
$packageJson = Get-Content $umbracoPackagePath -Raw | ConvertFrom-Json
$packageJson.Version = $fullVersion
$packageJson | ConvertTo-Json -Depth 32 | Set-Content $umbracoPackagePath

$sln_name = "..\uSync.sln";

# ""; "##### Restoring project"; "--------------------------------"; ""
# dotnet restore ..

""; "##### Building project"; "--------------------------------"; ""
dotnet build $sln_name -c $env -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true

""; "##### Generating the json schema"; "----------------------------------" ; ""
dotnet run -c $env --project ..\uSync.SchemaGenerator\uSync.SchemaGenerator.csproj --no-build

""; "##### Packaging"; "----------------------------------" ; ""
$projects = "uSync.Core", 
    "uSync.Community.Contrib",
    "uSync.Community.DataTypeSerializers",
    "uSync.BackOffice",
    "uSync.BackOffice.Targets",
    "uSync.Backoffice.Management.Api", 
    "uSync.Backoffice.Management.Client",
    "uSync";


foreach($project in $projects) {
    Write-Host "Packing $project : ";
    dotnet pack "..\$project\$project.csproj" --no-restore --no-build -c $env -o $outFolder -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true
}

if ($skipClient) {
    ""; "##### Skipping NPM Client Package"; "----------------------------------" ; ""
} 
else {

    ""; "##### Generating NPM Client Package"; "----------------------------------" ; ""
    Set-Location ..\uSync.Backoffice.Management.Client\usync-assets\

    npm version $fullVersion 
    npm run make

    Set-Location ..\..\dist
}

""; "##### Copying to LocalGit folder"; "----------------------------------" ; ""
XCOPY "$outFolder\*.nupkg" "C:\Source\localgit" /Q /Y 

if ($push) {
    ""; "##### Pushing to our nighly package feed"; "----------------------------------" ; ""
    nuget push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
    
    Remove-Item ".\last-push-*" 
    Out-File -FilePath ".\last-push-$fullVersion.txt" -InputObject $fullVersion
}

Write-Host "uSync Packaged : $fullVersion"

Remove-Item ".\last-build-*" 
Out-File -FilePath ".\last-build-$fullVersion.txt" -InputObject $fullVersion

Set-Clipboard -Value "dotnet add package uSync --version $fullVersion"
Write-Host "Dotnet command in clipboard"
## beep means i can look away :) 
[Console]::Beep(1056, 500)
