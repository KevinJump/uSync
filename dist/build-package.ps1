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
$finalRelease = $true;

if (![string]::IsNullOrWhiteSpace($suffix)) {
   $finalRelease = $false;
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

if ($skipClient) {
    ""; "##### Skipping NPM Client Package"; "----------------------------------" ; ""
} 
else {

    $clients = @( 
         "uSync.Backoffice.Management.Client\usync-assets", 
         "uSync.History\history-client" 
    )

    foreach($client in $clients) 
    {
        $isFullClient = $client -eq "uSync.Backoffice.Management.Client\usync-assets"

        ""; "##### Generating NPM Client Package"; "----------------------------------" 
        "#### $client"; "----------------------------------" ; ""
        Set-Location ..\$client

        npm version $fullVersion 
        ## npm run make

        ""; "'### Building uSync client"; "----------------------------------" ; ""
        ## build Client
        npm run build

        if ($isFullClient) 
        {
            ""; "### Build uSync Package"; "----------------------------------" ; ""
            ## build the npm package version 
            npm run client:build

            ""; "### Packaging uSync Package"; "----------------------------------" ; ""
            ## pack the package 
            npm run client:pack

            ""; "### Publishing uSync Package"; "----------------------------------" ; ""
            ## publish the package . 
            if (!$finalRelease) {
                npm publish --tag next 
            }
            else {
                npm publish --tag latest
            }
        }

        ## stamp the version, just means this value does not change per test build
        npm version $version;

        Set-Location ..\..\dist
    }
}


$sln_name = "..\uSync.slnx";

# ""; "##### Restoring project"; "--------------------------------"; ""
# dotnet restore ..

""; "##### Building project"; "--------------------------------"; ""
dotnet build $sln_name -c $env -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true

""; "##### Generating the json schema"; "----------------------------------" ; ""
dotnet run -c $env --project ..\uSync.SchemaGenerator\uSync.SchemaGenerator.csproj --no-build

""; "##### Packaging"; "----------------------------------" ; ""
$projects = "uSync.Core", 
    "uSync.AutoTemplates",
    "uSync.Community.Contrib",
    "uSync.Community.DataTypeSerializers",
    "uSync.BackOffice",
    "uSync.BackOffice.Targets",
    "uSync.Backoffice.Management.Api", 
    "uSync.Backoffice.Management.Client",
    "uSync.History",
    "uSync";

foreach($project in $projects) {
    Write-Host "Packing $project : ";
    dotnet pack "..\$project\$project.csproj" --no-restore --no-build -c $env -o $outFolder -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true
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
