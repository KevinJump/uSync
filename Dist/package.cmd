@echo off 

SET config=release
REM SET config=debug

CALL MsBuild .\uSync8.sln -t:Rebuild -p:Configuration=%config% -clp:Verbosity=minimal;Summary

@Echo Packaging for %config%
nuget pack .\uSync8.Core\uSync.Core.nuspec -build  -OutputDirectory .\dist\%1 -version %1 -properties "depends=%1;Configuration=%config%"
nuget pack .\uSync8.BackOffice\uSync.nuspec -build  -OutputDirectory .\dist\%1 -version %1 -properties "depends=%1;Configuration=%config%"
nuget pack .\uSync8.BackOffice\uSync.BackOffice.Core.nuspec -build  -OutputDirectory .\dist\%1 -version %1 -properties "depends=%1;Configuration=%config%"
nuget pack .\uSync8.Community.Contrib\uSync.Community.Contrib.nuspec  -OutputDirectory .\dist\%1 -build -version %1 -properties "depends=%1;Configuration=%config%"
nuget pack .\uSync8.ContentEdition\uSync.ContentEdition.nuspec  -OutputDirectory .\dist\%1 -build -version %1 -properties "depends=%1;Configuration=%config%"
nuget pack .\uSync8.ContentEdition\uSync.ContentEdition.Core.nuspec  -OutputDirectory .\dist\%1 -build -version %1 -properties "depends=%1;Configuration=%config%"

REM nuget pack .\uSync8.Community.DataTypeSerializers\uSync.Community.DataTypeSerializers.nuspec  -OutputDirectory .\dist\%1 -build -version %1 -properties "depends=%1;Configuration=%config%"
REM nuget pack .\uSync.Console\uSync.Console.nuspec  -OutputDirectory .\dist\%1 -build -version %1 -properties "depends=%1;Configuration=%config%"

nuget pack .\uSync8.HistoryView\uSync.History.nuspec  -OutputDirectory .\dist\%1 -build -version %1 -properties "depends=%1;Configuration=%config%"

call .\dist\CreatePackages %1

@ECHO Copying to LocalGit Folder
XCOPY .\dist\%1\*.nupkg c:\source\localgit /y

ECHO Packaging Complete (%config% build)

