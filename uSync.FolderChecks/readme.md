## uSync.FolderChecks

Checks a uSync folder on disk for problems that break (or quietly spoil) an import, without a site or a database:

- merge conflict markers, including inside CDATA values that still load as XML
- `.config` files that are not valid XML
- two items of the same type sharing a key, or an alias with different keys
- an item that is both deleted and still in the folder
- references to keys that no file in the folder defines

uSync uses it for the folder health check, and the uSync command line uses it for `files-verify`.

You don't need to install it yourself: the `uSync` package brings it in.

```csharp
var result = new SyncFolderVerifier().Verify("uSync/v17", new SyncFolderVerifyOptions { IncludeContent = true });
foreach (var issue in result.Issues)
    Console.WriteLine($"{issue.Rule.Id} {issue.FilePath}:{issue.Line} {issue.Message}");
```
