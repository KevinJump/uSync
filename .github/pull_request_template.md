## What does this change?

<!-- A sentence or two on the change and why it is needed. -->

## Notes for the reviewer

<!-- Anything non-obvious: behaviour changes, things you decided against, areas you want a
     second opinion on. Delete if there is nothing to say. -->

## Checklist

- [ ] `dotnet build uSync.slnx -c Release` is clean
- [ ] `dotnet test uSync.Tests/uSync.Tests.csproj -c Release` passes
- [ ] `CHANGELOG.md` updated under **Unreleased**
- [ ] If a dependency changed, `dotnet restore --force-evaluate` was run and the updated
      `packages.lock.json` is committed
- [ ] If a public API changed, it's called out in the changelog entry as an
      **Extender API** note
- [ ] Backoffice changes were clicked through in a running site, not just built
