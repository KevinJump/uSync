@echo off 

SET config=release
REM SET config=debug

CALL MsBuild .\uSync8.sln -t:Rebuild -p:Configuration=%config% -clp:Verbosity=minimal;Summary

nuget pack .\uSync.Triggers\uSync.Triggers.nuspec  -OutputDirectory .\dist\%1 -build -version %1 -properties "depends=%1;Configuration=%config%"

@ECHO Copying to LocalGit Folder
XCOPY .\dist\%1\*.nupkg c:\source\localgit /y

ECHO Packaging Complete (%config% build)

