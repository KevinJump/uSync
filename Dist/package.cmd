@echo off 

@Echo Packaging
nuget pack ..\uSync8.Core\uSync.Core.nuspec -build  -OutputDirectory %1 -version %1 -properties "depends=%1;Configuration=release"
nuget pack ..\uSync8.BackOffice\uSync.nuspec -build  -OutputDirectory %1 -version %1 -properties "depends=%1;Configuration=release"
nuget pack ..\uSync8.BackOffice\uSync.BackOffice.Core.nuspec -build  -OutputDirectory %1 -version %1 -properties "depends=%1;Configuration=release"
nuget pack ..\uSync8.Community.Contrib\uSync.Community.Contrib.nuspec  -OutputDirectory %1 -build -version %1 -properties "depends=%1;Configuration=release"
nuget pack ..\uSync8.ContentEdition\uSync.ContentEdition.nuspec  -OutputDirectory %1 -build -version %1 -properties "depends=%1;Configuration=release"
nuget pack ..\uSync8.ContentEdition\uSync.ContentEdition.Core.nuspec  -OutputDirectory %1 -build -version %1 -properties "depends=%1;Configuration=release"

call CreatePackages %1

XCOPY %1\*.nupkg c:\source\localgit /y