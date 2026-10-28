# Changelog

All notable code and behaviour changes to uSync are recorded here.

This file tracks changes to how uSync _behaves_. For changes to the on-disk
`.config` file format (which can cause items to report as changed and prompt a
re-export), see [`changes/format.md`](changes/format.md).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
History is backfilled from the v18 release history starting at `v18.0.0`.

## [19.0.0-beta01] - 2026-10-28

First beta of uSync for **Umbraco 19**, built against Umbraco 19.0.0-beta1 on
.NET 11.

### Added

- **Legacy folders can be migrated as they're copied.** When a pre-v19 uSync
  folder is copied into `uSync/v19`, the copy dialog lists the migrations that
  apply (multi node tree pickers, single media pickers and single dropdowns
  move to Umbraco 19's dedicated editors) and runs the ticked ones on the
  files, so the import that follows is a normal import. The legacy banner now
  points at the Migrate tab. (#1155)

- **Value and dependency mappers for the new v19 picker editors.**

- **Segment values sync as overrides of their culture**, and block expose
  entries duplicated by segment-era values are cleaned up on import.

- **A schedule's invariant-for-variant flag is exported and imported.**

- **Library elements referenced by blocks are tracked as dependencies.**

### Changed

- **uSync shows in its own Synchronisation section by default.** A one-off
  migration gives the section to the Admin group. Users without the section
  but with Settings access still see uSync in the Settings tree, and
  `uSync:Settings:MoveToSection: false` puts it back there. (#1156)

- **The uSync API is secured with the uSync tree policy.** It used to accept
  any user with the Content section and refuse users who only had the uSync
  section. It now accepts the Settings or uSync sections. (#1156)

- **Pre-v19 data type config is imported the way Umbraco 19 migrates it**,
  including moving a data type onto another editor when its config calls for
  it (e.g. a media picker with `multiple: false` onto the single media picker).

- **Imports run as the requesting or background user**, and content is saved
  and published as the file's writer or the importing user rather than the
  super user.

- **Blueprints are exported again when they or their folder move**, and
  imports fail cleanly for content types that can't have blueprints. Spaces in
  blueprint folder names created on import are kept.

- **Publish failure messages label the invalid properties.**

- **SignalR and the uSync API accept the backoffice cookie scheme**, matching
  Umbraco 19's cookie-based backoffice auth.

- **`@jumoo/usync` is published to npmjs.org** with trusted publishing.

### Removed

- **Members marked obsolete for removal in v18 or v19.** This is a breaking
  change for code that still calls them. Members marked for v20 stay. (#1161)
  - uSync.Core: the sync `IConfigurationSerializer.GetConfigurationExport/Import`
    defaults, `ConfigurationSerializerCollection.GetSerializer`,
    `ISyncMapper.GetImportValueAsync(value, editorAlias)` (the overload with
    `SyncSerializerOptions` is now required), `SyncAttempt.Succeed` without
    details, `AddNew(..., success)` and the `HandleTrashedState` overload
    without `relationAlias`.
  - uSync.BackOffice: the bulk notification constructors without a group,
    `ISyncActionService.StartProcessAsync(HandlerActions)` and
    `UnpackImportFromStream`, `ISyncService.FinishBulkProcessAsync` without a
    group, `ISyncFileService.VerifyFolder`, and `SyncHandlerRoot`'s
    `GetMergedItemsAsync(folders)` and `GetFolderKeys`.
  - uSync.Backoffice.Management.Api: `ISyncManagementService.GetActions`,
    `UnpackStream` and the `ToActionView` extension.

### Known issues

- uSync still uses `Constants.Security.SuperUserId`, which Umbraco 19 marks
  obsolete. It works on 19.0.0-beta1 and will be moved over in a later
  release. (#1162)

## [18.2.0] - 2026-10-26

### Added

- **The dashboard reconnects to uSync's SignalR hub automatically.** Before,
  a server restart or network blip cut off live progress until the page was
  reloaded. The connection now retries with a capped backoff, and it fetches a
  fresh access token on each reconnect. (#1135)

- **The server role is logged at startup.** uSync logs the resolved server role
  at Information level when it starts, which helps when diagnosing
  load-balanced setups. (#1134)

### Changed

- **`ImportSingle` returns a `uSyncActionView`.** It was the only Management
  API endpoint that returned a raw `uSyncAction`, which put `System.Exception`
  and the .NET reflection types behind it into the swagger document. It now
  returns the same view model as every other endpoint, with the exception
  message in `Message`. Exceptions elsewhere in the uSync swagger document are
  described as a simple object. Other packages' swagger documents are not
  touched. (#1116)

- **`uSyncSettings` has typed default values.** `DisableDashboard`,
  `SummaryDashboard` and `DisableNotificationSuppression` now default to
  booleans and `Folders` to an array in the swagger and appsettings schemas,
  where before they defaulted to strings. The `RootFolder` and `Folders` defaults
  no longer point at v14 folders. (#1116)

### Fixed

- **Rich text dependencies include URL-encoded local links.** Newer Umbraco
  versions resolve `%7BlocalLink:guid%7D` as well as `{localLink:guid}`
  ([umbraco/Umbraco-CMS#23767][u23767]). uSync now finds both forms when it
  works out an item's dependencies. (#1133)

- **Changing a content item's key cleans up its URL aliases.** When an import
  changed a content item's key, uSync removed the `umbracoDocumentUrl` rows for
  the old key but left the `umbracoDocumentUrlAlias` rows behind. It now removes
  those as well. (#1136)

- **Items with 1000 or more siblings import in the right order.** Import
  ordering packed level and sort order into one number, so an item with a sort
  order of 1000 or more could be imported before its own parent. Items are now
  sorted by level, then sort order. (#1119)

- **Faster, exact cache clearing during exports and reports.** uSync cleared
  cache entries with a prefix scan. That slowed large exports and reports, and it
  over-cleared: clearing id `12` also cleared `123`. Entries are now removed by
  their exact key. Ported from a contribution by @AndersKaae. (#1117)

[u23767]: https://github.com/umbraco/Umbraco-CMS/pull/23767

## [18.1.5] - 2026-09-30

### Fixed

- **Imported block values no longer show as having pending changes.** When an
  invariant block editor holds culture-variant elements, Umbraco re-serializes
  the value on publish and decides whether there are pending changes by
  comparing the draft and published values as strings. The draft uSync wrote
  differed in three ways: `editorAlias` was null, properties were in
  alphabetical order, and values were not sorted by culture. On import, uSync
  now sets each value's property type, sorts values by culture, and uses
  Umbraco's property order. Export output is unchanged. (#1098)

- **The published cache stays current after content type imports on Umbraco
  18.0–18.1.** These versions have two cache bugs
  ([umbraco/Umbraco-CMS#23433][u23433], [#23445][u23445]). After a composition
  changes, uSync now refreshes the content types that use it. When a content
  type is loaded for import, uSync clears its property types' stale dirty
  state, so a variance-only change is treated as a structural one. Umbraco
  18.2 fixes both bugs, and from 18.2 on the workarounds have no effect.
  (#1102)

- **Culture codes are exported in standard casing.** A language stored as
  `da-dk` could export as a mix of `da-dk` and `da-DK`, and files changed when
  Umbraco was upgraded. uSync now always writes the standard casing (`da-DK`),
  so output is the same on every version. Existing files with lower-case
  culture codes still import, but they may report as changed until they are
  re-exported. (#1101)

- The uSync History package's backoffice client now loads the uSync client by
  its package name (`@jumoo/usync`), so it resolves on case-sensitive file
  systems. (#1105)

[u23433]: https://github.com/umbraco/Umbraco-CMS/issues/23433
[u23445]: https://github.com/umbraco/Umbraco-CMS/issues/23445

## [18.1.0] - 2026-08-12

### Added

- **Opt-in import state cache — `uSync:Settings:CacheImportState` (default `false`).**
  uSync decides whether an item has changed by loading it from Umbraco, serializing
  the whole thing, and comparing hashes — for every item, every run. That means an
  import where nothing has changed costs roughly as much as a full export, which on
  sites with thousands of files is the entire run time.

  With this on, uSync remembers the hash of each file it has confirmed matches, and
  skips those items on the next run without a database lookup or a re-serialize.
  Import cost goes from `O(all items)` of database and serialization work to
  `O(changed items)`.

  It only ever remembers a file where the full check actually ran and said "no
  change", or that uSync has just exported (so the file was written from the
  database and the two match by construction). It never assumes that because an
  import succeeded the two sides now agree. **The practical effect is that the first
  run after turning it on is no faster than before — the benefit arrives on the
  second run.** An export warms it too.

  The cache lives in the site's temp folder (`{LocalTempPath}/uSync/cache/`), never
  in the uSync folder, and is thrown away whenever the database, the uSync version,
  or the handler settings change. Items are forgotten when Umbraco says they have
  been saved, deleted, moved or published; changing a doc type, data type, template,
  language or container clears the whole cache, because those get embedded in other
  items' xml. A force import always ignores it.

  What it cannot see is a database change made by something that raises no Umbraco
  notification — raw SQL, for example — hence the default of off. **Read
  [`docs/perf/state-cache.md`](docs/perf/state-cache.md) before enabling it**, in
  particular the limitations section, and the note for anyone writing a custom
  serializer that embeds data from another item.

  Implemented entirely through uSync's existing per-item notifications, so nothing
  in the import, report or serialization path changed.

- **Extender API:** the cancelable per-item notifications
  (`uSyncImportingItemNotification`, `uSyncReportingItemNotification`, and anything
  else deriving from `CancelableuSyncItemNotification<T>`) gain an optional
  `Message`, used instead of uSync's generic "change stopped by delegate event" when
  you set it. The import and report ones also gain `Force`, so a subscriber that
  short-cuts the check because it believes nothing has changed can stand down when
  the user has asked for a forced import.

### Changed

- Ported the v17 allocation work: fewer redundant dictionary lookups on the hot
  paths (#997), and `internal`/`private` classes are now `sealed` so the JIT can
  devirtualize their calls (#998). No behavioural changes.

  > **Extender API:** `SyncHandlerRoot.SyncChangeInfo` is now `sealed`. It stays
  > `protected`, so handlers can still construct and return one from an
  > `IsItemCurrentAsync` override — only deriving from it is no longer possible.

- **Handler settings now inherit from `HandlerDefaults`.** When a handler has its
  own settings block, its values are layered _over_ the set's `HandlerDefaults`
  instead of replacing them wholesale. A handler now only needs to specify the
  settings it wants to change from the defaults.
  - The additional `Settings` dictionary is merged key-by-key (the handler's own
    keys win), so a per-key default such as `CreateOnly` set in `HandlerDefaults`
    now cascades to handlers that define their own block.
  - The strongly typed properties (`UseFlatStructure`, `GuidNames`, etc.) also
    cascade, via a new `IServiceCollection.ConfigureHandlerSet(...)` extension
    that binds each handler's configuration on top of a clone of the defaults at
    options-binding time.

  > **Breaking:** Previously a handler that defined its own block ignored
  > `HandlerDefaults` entirely. Configurations that relied on that replacement
  > behaviour (i.e. expected a handler block to _reset_ settings back to their
  > built-in defaults rather than inherit the set defaults) will now see the
  > inherited values instead. Review any set that mixes `HandlerDefaults` with
  > per-handler blocks.

- **Extender API:** `ISyncManagementService` gains `UnpackStreamAsync(Stream)`.
  The synchronous `UnpackStream(Stream)` is now obsolete (removed in v19). (#1005)

- Cleared the remaining build warnings left over from the Umbraco 18 upgrade:
  replaced `ITemplate.MasterTemplateAlias` usage with `LayoutTemplateAlias`,
  and inlined the legacy `{localLink:x}` parsing that Umbraco is removing in
  v18 into uSync's own code, since uSync still needs to detect un-migrated
  links. No behavioural changes. (#1002, #1003, #1004)

- **JSON helpers now come from the `Jumoo.Json` package.** uSync's
  `JsonTextExtensions` was a copy of that library, and had drifted behind it.
  uSync's own code now calls `Jumoo.Json` directly, so it picks up the
  correctness and allocation work done there.

  `uSync.Core.Extensions.JsonTextExtensions` is **still there and still
  behaves the same**, so nothing downstream needs to change — but every method
  on it is now `[Obsolete]` and forwards to `Jumoo.Json`. They will be removed
  in v20. To move over, replace `using uSync.Core.Extensions;` with
  `using Jumoo.Json;`; note that a file can't have both, because they declare
  the same extension method signatures (CS0121). A few names differ:
  `TryGetPropertyAsObject` → `TryGetPropertyAsJsonObject` and
  `GetPropertyAsObject` → `GetPropertyAsJsonObject`, and the methods that
  returned `string.Empty` for a missing or null value now return `null`.

  `uSync.Core.Json.JsonXElementConverter` is obsolete for the same reason
  (use `Jumoo.Json.Converters.JsonXElementConverter`).

- **Removed three O(n²) lookups from import.** All of them only showed up on
  large syncs, and none of them change what is imported:
  - the duplicate key check when merging folders, and the "keys to keep" check
    when deleting missing items during a clean, are now set lookups rather than
    a scan of a list per item.
  - second pass imports (content, media) now index the action list once, instead
    of scanning it — and building two interpolated strings per comparison — for
    every item. With 10,000 two pass items that was tens of millions of string
    allocations.

  > Actions updated by a second pass are now updated in place, so they keep
  > their original position in the results list. Previously each one was removed
  > and re-added, which moved it to the end of the handler's list. Only the
  > display/reporting order is affected.

  **Extender API:** `List<uSyncAction>.CreateActionIndex()` and the matching
  `UpdateActions(index, key, handlerAlias, attempt)` overload are new. The
  existing `UpdateActions(key, handlerAlias, attempt)` is unchanged, including
  its move-to-the-end behaviour.

### Fixed

- **Content and Library items are no longer exported twice by one editor
  action.** Umbraco 18.1 raises the saved notification for a save-and-publish as
  well as the published one ([umbraco/Umbraco-CMS#23523][u23523]); uSync
  listens to both, so a single save-and-publish serialized and wrote the item
  twice, and un-publishing a culture could write it three times. All the
  notifications for one operation now share a record of which items have been
  exported, so each item is exported once. Publishing or un-publishing from the
  tree — which still only raises the publish notification — is unaffected.

  This applies to both the Content and the Library (Element) handlers.

  [u23523]: https://github.com/umbraco/Umbraco-CMS/issues/23523

- `HandlerSettings.Clone()` no longer drops the `CreateClean` and
  `FullFileOnDifference` properties. Handlers that set either value in their own
  block were previously resolved as `false` regardless; they are now honoured.

- Property values containing a quote, backslash or control character were not
  converted to JSON at all — the string fallback was built by quoting the value
  into a JSON literal, which is invalid JSON for those inputs. Inherited with
  the move to `Jumoo.Json`.

- Serializing, comparing and expanding large property values allocated far more
  than they needed to, several of them on the large object heap. Also inherited
  with the move to `Jumoo.Json` — see that package's benchmarks for the numbers.

## [18.0.3] - 2026-07-22

### Fixed

- Culture-variant property values were dropped on import because of a
  `culture`/`cultures` typo. Variant properties now import correctly. (#1000)

## [18.0.2] - 2026-07-13

### Added

- `ISyncContainerHandler` — lets a handler whose items live in container (folder)
  items export those containers one at a time. The Library **Element** handler
  implements it, so container folders are no longer left behind when items are
  exported individually (e.g. a dependency-based push); previously the folders
  were only written during a full export. (#980)

### Changed

- **Extender API:** `ISyncContainerHandler.ExportContainer` now takes a `Udi`.

### Fixed

- Element container nodes now resolve to the Element handler by type name.
- Merged the latest `v17/main` fixes and performance improvements into v18.

## [18.0.0] - 2026-06-25

### Added

- Initial uSync release for **Umbraco 18**.
