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

### Fixed

- `HandlerSettings.Clone()` no longer drops the `CreateClean` and
  `FullFileOnDifference` properties. Handlers that set either value in their own
  block were previously resolved as `false` regardless; they are now honoured.

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
