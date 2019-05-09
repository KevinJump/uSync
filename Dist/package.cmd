@echo off 

@Echo Packaging
nuget pack ..\uSync8.Core\uSync.Core.nuspec -build  -OutputDirectory %1 -version %1  -properties depends=%1
nuget pack ..\uSync8.BackOffice\uSync.nuspec -build  -OutputDirectory %1 -version %1 -properties depends=%1
nuget pack ..\uSync8.BackOffice\uSync.BackOffice.Core.nuspec -build  -OutputDirectory %1 -version %1 -properties depends=%1
nuget pack ..\uSync8.ContentEdition\uSync.ContentEdition.nuspec  -OutputDirectory %1 -build -version %1  -properties depends=%1


CALL UmbPackage package.xml %1
CALL "c:\Program Files\7-Zip\7z.exe" a .\%1\uSync_%1.zip .\%1\uSync_%1\*.* 
CALL RD .\%1\uSync_%1\ /s /q

CALL UmbPackage package.ContentEdition.xml %1
CALL "c:\Program Files\7-Zip\7z.exe" a .\%1\uSync.ContentEdition_%1.zip .\%1\uSync.ContentEdition_%1\*.* 
CALL RD .\%1\uSync.ContentEdition_%1\ /s /q

XCOPY %1\*.nupkg c:\source\localgit /y