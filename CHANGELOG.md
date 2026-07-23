# Changelog

All notable code and behaviour changes to uSync are recorded here.

This file tracks changes to how uSync *behaves*. For changes to the on-disk
`.config` file format (which can cause items to report as changed and prompt a
re-export), see [`changes/format.md`](changes/format.md).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed

- **Handler settings now inherit from `HandlerDefaults`.** When a handler has its
  own settings block, its values are layered *over* the set's `HandlerDefaults`
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
  > behaviour (i.e. expected a handler block to *reset* settings back to their
  > built-in defaults rather than inherit the set defaults) will now see the
  > inherited values instead. Review any set that mixes `HandlerDefaults` with
  > per-handler blocks.

### Fixed

- `HandlerSettings.Clone()` no longer drops the `CreateClean` and
  `FullFileOnDifference` properties. Handlers that set either value in their own
  block were previously resolved as `false` regardless; they are now honoured.
