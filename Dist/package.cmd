
@Echo Packaging
nuget pack ..\uSync8.BackOffice\uSync.nuspec -build -Properties version="%1"
nuget pack ..\uSync8.Core\uSync.Core.nuspec -build -Properties version="%1"