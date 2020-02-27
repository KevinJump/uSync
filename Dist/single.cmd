@REM build a single
@REM single [project] [nuspec] [version] [depends]
@REM single uSync8.Community.Contrib uSync.Community.Contrib 8.5.0.1 8.5.1
 
@echo off 

@Echo Packaging
nuget pack ..\%1\%2.nuspec -build  -OutputDirectory %3 -version %3 -properties "depends=%4;Configuration=release"

XCOPY %3\*.nupkg c:\source\localgit /y