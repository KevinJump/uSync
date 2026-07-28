# Changelog

All notable code and behaviour changes to uSync are recorded here.

This file tracks changes to how uSync _behaves_. For changes to the on-disk
`.config` file format (which can cause items to report as changed and prompt a
re-export), see [`changes/format.md`](changes/format.md).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
History is backfilled from the v18 release history starting at `v18.0.0`.

## [Unreleased]

### Changed

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

### Fixed

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
