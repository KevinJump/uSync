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

$sln_name = "..\uSync_13.sln";

""; "##### Restoring project"; "--------------------------------"; ""
dotnet restore ..

""; "##### Building project"; "--------------------------------"; ""
dotnet build $sln_name -c $env -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true

""; "##### Generating the json schema"; "----------------------------------" ; ""
dotnet run -c $env --project ..\uSync.SchemaGenerator\uSync.SchemaGenerator.csproj --no-build

""; "##### Packaging"; "----------------------------------" ; ""

dotnet pack ..\uSync.Core\uSync.Core.csproj --no-restore --no-build -c $env -o $outFolder -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true
dotnet pack ..\uSync.Community.Contrib\uSync.Community.Contrib.csproj  --no-build  --no-restore -c $env -o $outFolder -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true
dotnet pack ..\uSync.Community.DataTypeSerializers\uSync.Community.DataTypeSerializers.csproj  --no-build  --no-restore -c $env -o $outFolder  -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj  --no-build  --no-restore -c $env -o $outFolder  -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true
dotnet pack ..\uSync.History\uSync.History.csproj  --no-build  --no-restore -c $env -o $outFolder  -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true

dotnet pack ..\uSync.AutoTemplates\uSync.AutoTemplates.csproj --no-build --no-restore -c $env -o $outFolder  -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true

dotnet pack ..\uSync.BackOffice.Targets\uSync.BackOffice.Targets.csproj --no-build --no-restore -c $env -o $outFolder  -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true

dotnet pack ..\uSync\uSync.csproj  --no-build  --no-restore -c $env -o $outFolder  -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true
# .\nuget pack "..\uSync\uSync.nuspec" -version $fullVersion -OutputDirectory $outFolder


#Get-ChildItem -Path ..\uSync.BackOffice.Assets\wwwroot -Include * -File -Recurse | foreach { $_.Delete()}
#&gulp minify --release $version

dotnet pack ..\uSync.BackOffice.Assets\uSync.BackOffice.Assets.csproj --no-restore -c $env -o $outFolder  -p:Version=$fullVersion -p:ContinuousIntegrationBuild=true

""; "##### Copying to LocalGit folder"; "----------------------------------" ; ""
XCOPY "$outFolder\*.nupkg" "C:\Source\localgit" /Q /Y 

if ($push) {
    ""; "##### Pushing to our nighly package feed"; "----------------------------------" ; ""
    .\nuget.exe push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
    
    Remove-Item ".\last-push-*" 
    Out-File -FilePath ".\last-push-$fullVersion.txt" -InputObject $fullVersion
}

Write-Host "uSync Packaged : $fullVersion"

Remove-Item ".\last-build-*" 
Out-File -FilePath ".\last-build-$fullVersion.txt" -InputObject $fullVersion

## beep means i can look away :) 
[Console]::Beep(1056, 500)
