function usync() {
    Write-Host 'uSync Cli.';

    $project = Get-Project
    $projectDir = Split-Path $project.FullName 
    $uSyncExe = Join-Path $projectDir 'bin\usync.exe'

    if (Test-Path $uSyncExe) {
        & $uSyncExe $args
    }
    else {
        Write-Host "Cannot find uSync.exe in the project's bin folder"
    }
}

Export-ModuleMember usync