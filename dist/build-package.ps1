param ($suffix, $env='release')

dotnet pack ..\uSync.Core\uSync.Core.csproj -c $env -o .\nightly --version-suffix $suffix
dotnet pack ..\uSync.BackOffice\uSync.BackOffice.csproj -c $env -o .\nightly --version-suffix $suffix
