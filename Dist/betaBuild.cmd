@ECHO OFF

nuget pack ..\uSync8.Core\uSync.Core.nuspec -build  -OutputDirectory beta\%1 -version %1  -properties depends=%1
nuget pack ..\uSync8.BackOffice\uSync.nuspec -build  -OutputDirectory beta\%1 -version %1 -properties depends=%1
nuget pack ..\uSync8.BackOffice\uSync.BackOffice.Core.nuspec -build  -OutputDirectory beta\%1 -version %1 -properties depends=%1
nuget pack ..\uSync8.ContentEdition\uSync.ContentEdition.nuspec  -OutputDirectory beta\%1 -build -version %1  -properties depends=%1

XCOPY beta\%1\*.nupkg c:\source\localgit\uSyncBuilds\ /y
	
