param(
    # version to build
    [Parameter(Mandatory)]
    [string]
    $version,

    # suffix to add to version (e.g beta001)
    [string]
    $suffix,

    # what configuration to build
    [string]
    $config = 'release',

    # push the package to the nightly nuget.
    [switch]
    $push = $false
)

$versionString = $version
if (![string]::IsNullOrWhiteSpace($suffix)) {
    $versionString = -join($version, '-', $suffix)
}

$outfolder = ".\$version\$versionString" 

"------------------------------------------------"
Write-Host "Version   :" $versionString
Write-Host "Output    :" $outfolder
"------------------------------------------------"

$buildArgs = @('..\uSync8.sln', '-t:Rebuild', "-p:Configuration=$config", "-clp:Verbosity=q;Summary", "-m")
& MSBuild.exe $buildArgs

if (!$?) { Write-Host "Build failed" $LASTEXITCODE; exit  }

## pack
""; "##### Packaging nugets"; "----------------------------------"

$specs = @(
    'uSync8.Core\uSync.Core.nuspec',
    'uSync8.BackOffice\uSync.nuspec',
    'uSync8.BackOffice\uSync.BackOffice.Core.nuspec',
    'uSync8.Community.Contrib\uSync.Community.Contrib.nuspec',
    'uSync8.ContentEdition\uSync.ContentEdition.nuspec',
    'uSync8.ContentEdition\uSync.ContentEdition.Core.nuspec',
    'uSync8.HistoryView\uSync.History.nuspec'
)

$properties = "depends=$versionString;Configuration=$config"

foreach($spec in $specs) {
    Write-Host "Packing: " $spec
    .\nuget.exe pack ..\$spec -build -OutputDirectory $outFolder -version $versionString -properties "$properties" -Verbosity quiet 
    if (!$?) { Write-Host "Packing $spec failed" $LASTEXITCODE; exit }
}

""; "##### Creating the Umbraco Packages"; "----------------------------------"
.\createpackages.cmd $versionString $outfolder
if (!$?) { Write-Host "Create Umbraco package failed" $LASTEXITCODE; exit  }

# copy to local
""; "##### Copying to LocalGit folder"; "----------------------------------" 
$result = Copy-Item -Path $outFolder\*.nupkg -Destination C:\Source\localgit -PassThru
Write-Host "Copied " $result.length "items to localgit"

# push to azure nightly
if ($push) {
	""; "##### Pushing to our nighly package feed"; "----------------------------------" ; ""
	.\nuget.exe push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}

"" ; "--------------------------------------------------------------------" 
Write-Host "uSync Packaged : $versionString"
"--------------------------------------------------------------------" 

