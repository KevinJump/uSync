@ECHO OFF

nuget pack ..\uSync8.Core\uSync.Core.nuspec -build  -OutputDirectory %1 -version %1  -properties depends=%1

XCOPY %1\*.nupkg c:\source\localgit /y

