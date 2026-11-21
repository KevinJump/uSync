# Key mapping (Umbraco 18+)

Status: built. Step 1 (key-conflict error) shipped in #1276; steps 2-4
(`KeyConflict: Map`) are built as described below, with the changes listed
under [As built](#as-built).

## Problem

From Umbraco 18 a saved entity's `Key` can't change
(`EntityBase.Key` setter throws "Cannot change the Key of an existing …",
umbraco/Umbraco-CMS#21131). Only `File` entities (templates, scripts,
stylesheets, partial views) override `CanChangeKey`.

uSync finds items by key, then falls back to name/alias
(`SyncSerializerRoot.FindItemAsync(XElement)`). When the fallback finds an
item whose key differs from the file, the serializer sets `item.Key = key` and
the import fails. Affected setters:

| Where | Entity |
|---|---|
| `ContentSerializerBase.cs:488` | content, media, members |
| `ContentTypeBaseSerializer.cs:271` | doc/media/member types |
| `ContentTypeBaseSerializer.cs:495` | property types (`PropertyType : EntityBase`) |
| `ContentTypeBaseSerializer.cs:890` | tabs/groups (`PropertyGroup : EntityBase`) |
| `DataTypeSerializer.cs:112` | data types |
| `DictionaryItemSerializer.cs:86` | dictionary |
| `RelationTypeSerializer.cs:52` | relation types |
| `WebhookSerializer.cs:87` | webhooks |
| `SyncContainerSerializerBase.cs:117` | containers |

Writing the key straight to `umbracoNode.uniqueId` isn't an option. In v18
the key is also stored in document URLs, navigation, redirect URLs, granular
permissions, webhook keys and the published cache. References to the old key
created on the target would break too.

## Setting: `KeyConflict`

| Value | Behaviour |
|---|---|
| `Set` (default, every version) | Today's behaviour: set the key. On v18+ the conflict fails with a clear key-conflict error (below). No map, pre-scan or guid replacement. |
| `Map` | Opt in. Turns on everything in [Key map](#key-map-keyconflict-map). |

Most sites never hit a conflict, so the default must not cost them anything.
With `Set`, none of the map code runs. `ISyncKeyMap` resolves to a null
implementation, the pre-scan isn't called, and nodes reach the serializers
untouched. Choose the implementation once, from the setting, at composition
time or when the import or report starts, not per item, so the `Set` path
has no extra branches in the hot loop.

The setting is global. A per-handler override can be added later if needed.
`Recreate` (delete the content or media item and create it with the file
key) is a possible later value. It loses history, ids and references on the
target, so it would only ever be opt-in, and never for doctypes or data types.

## Key-conflict error (`Set` mode)

Today a key conflict comes out as a bare "Import Fail: Cannot change the Key
of an existing Content". Replace that with an error that says what it is.

- Add one helper, `SyncKeyHelper.SetKey(IEntity item, Guid key, string alias)`,
  and use it in place of every `item.Key = key` in the table above.
- When `item.HasIdentity && item.Key != key`, it tries the set. If it
  catches the `InvalidOperationException`, it throws
  `uSyncKeyConflictException(itemType, alias, localKey, fileKey)` with the
  original exception as its inner exception.
- Check the identity, don't match the message text, so a wording change in
  Umbraco doesn't break it. The `catch` stays, so v17 (where the set works)
  runs the same code.
- Message, for example:
  `Key conflict: 'Home' (Content) exists on this site with key {local}, but the file has key {file}. Umbraco no longer allows keys to change. See https://docs.jumoo.co.uk/usync/…/key-conflicts`
  The docs page explains `KeyConflict: Map`, not the error message.
- The handler's existing `catch` (`SyncHandlerRoot.ImportElementAsync`)
  already turns `ex.Message` into a failed `uSyncAction`, so the message
  reaches the UI and the logs without more plumbing. Consider a distinct
  `ChangeType`/flag so the dashboard can group key conflicts and link to the
  docs.
- The report can predict it: in `Set` mode on v18, when `FindItemAsync`
  matches by name and the keys differ, report a key-conflict warning instead
  of a plain update. That's a one-line check on a path that already runs, with
  no scan.

## Key map (`KeyConflict: Map`)

Each site keeps its own map. It's a per-instance problem: the map is never
exported, pushed or pulled, and Publisher/Complete don't exchange it.

Before a serializer sees an XML node, swap any mapped guid in the node for
its local key. After that, the item's own file carries the local key, so
`SetKey` has nothing to change. References to the item in other files get
rewritten the same way, so the hash compare in `IsCurrentAsync` matches and
reports come out clean.

Guids are globally unique, so swapping them by string match across a whole
node is safe. Only guids already in the map get replaced. Block content keys
and other random guids pass through unchanged, and no mapper changes are
needed.

```
file on disk ──load/merge──► XElement ──Localize()──► serializer
  (K_file)                                (K_local)
                                              ▲
                              ISyncKeyMap ────┘  (db table + memory cache)
```

Files on disk are never rewritten. Source control stays clean and the
handlers' tracking by key keeps working.

### `ISyncKeyMap` (uSync.Core)

```csharp
public interface ISyncKeyMap
{
    bool Enabled { get; }                                  // false unless KeyConflict = Map
    bool TryGetLocal(Guid fileKey, out Guid localKey);
    Task AddAsync(string entityType, Guid fileKey, Guid localKey);
    Task RemoveAsync(Guid fileKey);
    Task RemoveLocalAsync(Guid localKey);                  // local item deleted
    XElement Localize(XElement node);                      // returns node unchanged when nothing maps
}
```

- `NullSyncKeyMap` is used in `Set` mode and in Core-only installs:
  `Enabled = false`, `Localize` returns its input, and `Add`/`Remove` do
  nothing.
- `SyncKeyMap` (BackOffice) uses a new `uSyncKeyMap` table (`FileKey` PK,
  `LocalKey` unique, `EntityType`, `Created`). It's added through the
  existing migration plan (`uSync.BackOffice/Boot`). The table is created on
  every site but stays empty unless `Map` is on.
- Cache: the whole table goes into a `Dictionary<Guid, Guid>` on first use
  and is cleared when `StartBulkProcessAsync` runs for import or report. The
  table is expected to stay small.
- `Localize`: return straight away when the map is empty. Otherwise do one
  regex pass over attribute values and text nodes with a pattern that
  matches both `D` (dashed) and `N` (no dashes, as in `umb://document/<N>`
  UDIs) guids, any case. Look each match up and write the local key back in
  the same format. Clone the node only on the first hit.

### Discovery

`SyncSerializerRoot.FindItemAsync(XElement)` is where a key miss with a name
hit already happens:

```csharp
var byAlias = await FindItemAsync(alias);
if (keyMap.Enabled && byAlias != null && key != Guid.Empty && ItemKey(byAlias) != key)
    await keyMap.AddAsync(ItemType, key, ItemKey(byAlias));
return byAlias;
```

This covers an item's own file and any file processed after it.

### Localize at the serializer entry points

Call `keyMap.Localize(node)` at the top of the public node-taking methods on
`SyncSerializerRoot`: `DeserializeAsync`, `DeserializeSecondPassAsync`,
`IsCurrentAsync(XElement, …)` and `FindItemAsync(XElement)`. Putting it in
the serializer root rather than in `SyncFileService` covers every caller:
handler import and report, paged UI imports, second passes (which reload the
file, `SyncHandlerRoot.cs:597`), and Publisher/Complete push and pull, which
hand nodes to the serializers directly.

`Localize` only replaces mapped file keys with local keys, so calling it
twice gives the same result. Chains can't form: a file key that exists
locally is found by key and never mapped.

### Pre-scan

Discovery alone misses forward references. Data types import before
doctypes, and block list/grid configs reference element type keys. If an
element type is matched by name later in the run, the data type has already
imported with the stale key.

Add `ISyncHandler.ScanKeyConflictsAsync(string[] folders)` (default
implementation in `SyncHandlerRoot`). It's only called when
`keyMap.Enabled`:

1. `GetMergedItemsAsync(folders)`. This is the same merge the import does,
   and it should come from the merge cache, so the import doesn't read the
   files a second time.
2. Check which file keys exist locally in one query per object type
   (`IEntityService.GetAll(objectType, keys)`, with the first-import lookup
   cache from #1271).
3. Only for the misses, call `serializer.FindItemAsync(node)`. Discovery
   then records the mapping.

Run it over every handler in the set before any import or report:

- `SyncService.ImportAsync`, before the handler loop (startup and CLI path).
- `SyncActionService.StartProcessAsync` for Import and Report. This needs the
  folders and handler set on `SyncStartActionRequest`.

### Serializer changes in `Map` mode

- Content, data types and the other node-level types need no change: the
  node arrives localized, so `SetKey` does nothing.
- Property types and tabs: these match by alias inside the doctype's own
  file, so the doctype node can't be localized ahead of time. In
  `ContentTypeBaseSerializer`, when `Map` is on, the alias matches and the
  keys differ, add the mapping and skip `SetKey`. In `Set` mode they go
  through `SetKey` and get the key-conflict error like everything else.
- Record a "key mapped" `uSyncChange` so reports show the mapping.

### Map hygiene

| Event | Action |
|---|---|
| Item with the file key now exists locally | `FindItemAsync(key)` hits first, so remove the stale entry when you see it |
| Mapped local item deleted (handler `DeletedNotification`) | `RemoveLocalAsync` (only when `Enabled`) |
| Name match ambiguous (two siblings, neither key matches) | don't map, fail with the key-conflict error |
| Delete/rename empty files | keyed by file key, `Localize` handles them |
| Export | unchanged: writes local keys |
| `Map` switched back to `Set` | the table stays, unused. Switching back to `Map` picks it up again |

A wrong name match persists once it's in the map, so the dashboard needs
a way to view and clear entries (and a health check that lists them) when
`Map` is on.

## Rollout

Build it on v17, where it does nothing unless `Map` is turned on (v17 can
set keys anyway), and forward-port it, following the branch policy. The
key-conflict error is the most useful part on v18 and can ship before the
map.

1. `SyncKeyHelper.SetKey` + `uSyncKeyConflictException` + the report warning
   + the docs page.
2. `KeyConflict` setting, `ISyncKeyMap` with the null implementation, the
   table, discovery and `Localize`.
3. Pre-scan, plus the start request carrying folders and handlers.
4. Dashboard view and clear, and the health check.

## Open questions

1. Does the UI start request know the folders and handler set before the
   first handler runs? If not, the pre-scan runs inside the first handler
   call.
2. Can the pre-scan reuse the merged nodes rather than merging again? The
   handlers already cache in `PrepCaches`.
3. Regex cost on large content files when the map isn't empty. A fallback is
   to only run it when the raw XML contains one of the mapped keys.
4. Partial (culture/segment) files are generated for push/pull and pass
   through the same serializer entry points. Confirm Publisher doesn't
   compare raw XML before deserializing.

## Test cases

- `Set` (default) on v18, name match with a different key: the import fails
  with the key-conflict message and docs link, and the report shows a
  key-conflict warning.
- `Set` on v17: the key is set as today, with no behaviour change.
- `Set`: no pre-scan, no `Localize` and no map queries. The perf numbers
  match the baselines in `docs/perf`.
- `Map`, fresh target with the content tree created by hand using the same
  names: the first import maps every node, nothing throws, and a second
  report shows no changes.
- `Map`, element type matched by name and referenced from a block list data
  type in the same run: the data type config ends up with the local key
  (proves the pre-scan).
- `Map`, content picker, MNTP, multi URL, RTE `localLink` and block values
  that reference a mapped item: all rewritten (covers `D` and `N` formats).
- `Map`, mapped item deleted locally, then the import runs again: the entry
  is removed and the item is created with the file key.
- Two siblings with the same name: a key-conflict error, no mapping.

## As built

Where the build differs from the design above:

- **Storage**: no new table. The map is one JSON value in Umbraco's
  `umbracoKeyValue` table (key `uSync_KeyMap`), loaded whole on first use and
  written back whole on change. No migration, so first-boot imports can use it
  too. Writes are held while a batch is open (`ISyncKeyMap.BeginBatch`): the
  scan and `ImportAllAsync` each write once.
- **Where it runs**: in the handlers (`SyncHandlerRoot`), not the serializer
  root. Handlers reach the map through `ISyncItemFactory.KeyMap`, so no
  handler or serializer constructors changed. Localize + discovery run at
  `ImportSingleElementAsync`, `ReportElementSingleAsync`, `BuildPreviewAsync`,
  and Localize at `ImportSecondPassAsync`. Every caller (UI, startup,
  Publisher, TimeMachine) goes through these.
- **Discovery** uses a new `ISyncSerializer.FindItemByNameAsync(node)`.
  Content's `FindItemAsync(node)` only matches by key, and its name match lives
  in `FindOrCreateAsync`, so the new method copies that match (by name under
  the parent). Content at the root isn't matched by name, as before.
- **Property types, tabs and containers**: `SyncKeyHelper.SetKey` checks an
  ambient scope (`SyncKeyHelper.BeginKeyMapScope`, an `AsyncLocal`) that the
  handler opens around each item. Inside it, a saved item with a different key
  is mapped instead of changed, so these are mapped as they're imported and
  swapped on the next run. This also applies on v17 when `Map` is on: `Map`
  means map, whatever the Umbraco version.
- **Clean**: keys read from files for clean/delete-missing checks go through
  the map (`ToLocalKey`), so mapped items aren't deleted as "missing".
- **Pre-scan**: `ISyncService.ScanKeyConflictsAsync(folders, handlers)`. It runs
  before the handler loop in `SyncService.ImportAsync`. In the UI
  (`SyncActionService`) the start request marks a scan as pending, and the
  first Import/Report handler request runs it over every handler in the
  set/group, so the client didn't change. Production (single file) folders are
  scanned too.
- **Paged imports** (`ImportPartialAsync`/`ReportPartialAsync`, used by
  Publisher) get per-item discovery only, with no pre-scan. Forward references
  in a first paged import can be missed until the next run.
- **View/clear**: a health check ("uSync - Key map") lists the mappings and has
  actions to remove one mapping or clear them all. There's no dashboard UI.
- **Ambiguous name matches** aren't detected. The first match wins, as in
  `FindOrCreateAsync`.
