param ($suffix, $env='release')

dotnet pack ..\uSync.Core\uSync.Core.csproj -c $env -o .\nightly --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync.Community.Contrib\uSync.Community.Contrib.csproj -c $env -o .\nightly --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj -c $env -o .\nightly --version-suffix $suffix /p:ContinuousIntegrationBuild=true 
