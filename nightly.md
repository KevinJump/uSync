## uSync Nightly feed.

uSync final releases and Release candidates will be published to nuget.org. So you shouldn't need to do anything special to get these packages. 

to try some of the more experimental features, and keep upto date with new developements you can use packages from our nightly build feed. 

```
https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
```

### nuget.config file:

You can add the source to a nuget.config file in the root of your solution, 
when adding / updating package dotnet will add this to the list of sources. 

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Jumoo Nightly" value="https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json" />
  </packageSources> 
</configuration>
```

### Add to nuget sources
you can add the nightly feed to your list of nuget sources

```
dotnet nuget add source -n "JumooNightly" https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
```
