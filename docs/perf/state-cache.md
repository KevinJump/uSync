# The import state cache (`CacheImportState`)

**Setting:** `uSync:Settings:CacheImportState` — **default `false`**

An opt-in cache that lets a repeat import or report skip items it has already confirmed match
Umbraco, without a database lookup or a re-serialize.

---

## 1. The problem it solves

uSync works out whether an item has changed by loading it from Umbraco, serializing the whole
thing, and comparing hashes:

[`SyncSerializerRoot.DeserializeAsync`](../../uSync.Core/Serialization/SyncSerializerRoot.cs)
calls `IsCurrentAsync` for **every** item, and
[`IsCurrentAsync`](../../uSync.Core/Serialization/SyncSerializerRoot.cs) does:

```
FindItemAsync(node)          // database lookup
  -> SerializeAsync(item)    // full re-serialize of the Umbraco item
  -> CleanseNode() x2        // (some serializers deep-clone here)
  -> two hashes              // one per side
```

The report path pays the same cost through
[`SyncHandlerRoot.IsItemCurrentAsync`](../../uSync.BackOffice/SyncHandlers/SyncHandlerRoot.cs).

The consequence is that an import where **nothing has changed** costs roughly as much as a full
export. Reading the files is not the bottleneck — that is already parallelised in
[`SyncFileService.GetFolderItemsAsync`](../../uSync.BackOffice/Services/SyncFileService.cs) — the
per-item work afterwards is. On a site with thousands of content and dictionary files that per-item
work is the whole run time.

With the cache on, an item we have already confirmed costs one hash of an `XElement` we have
already parsed. Import cost goes from `O(all items)` of database and serialization work to
`O(changed items)`.

## 2. How it hooks in

Through notifications, not by changing the import pipeline. uSync already fires a cancelable
notification per item on both the import and report paths — the report one exists for exactly this
purpose ("this lets us intercept a report and shortcut the checking (sometimes)"). So the whole
feature sits off to one side:

| Notification | What we do |
|---|---|
| `uSyncImportingItemNotification` / `uSyncReportingItemNotification` | cancel the item if we already know the file matches |
| `uSyncImportedItemNotification` / `uSyncReportedItemNotification` | record the file's hash **if the answer was `NoChange`** |
| `uSyncExportedItemNotification` | record the file's hash — we just wrote it from the database |
| `uSync*Starting` / `uSync*Completed` | load / save the cache file |

Nothing in `SyncSerializerRoot`, no serializer, and none of the handler import or report methods
changed. The feature is two classes
([`SyncStateCacheManager`](../../uSync.BackOffice/Cache/SyncStateCacheManager.cs),
[`SyncStateCacheInvalidator`](../../uSync.BackOffice/Cache/SyncStateCacheInvalidator.cs)) plus the
cache itself ([`SyncStateCache`](../../uSync.BackOffice/Cache/SyncStateCache.cs)) and one setting.

### Only confirmed matches are recorded

An entry is written in exactly two situations:

1. the full expensive check ran and returned `NoChange`, or
2. we have just exported the item, so the file was written from the database and the two sides
   match by construction.

We deliberately **do not** record after a successful update or create. It is tempting to assume
the two sides now agree, but they do not always: some items do not round-trip exactly, which is
what uSync's *"XML is different - but properties may not have changed"* message is telling you.
Recording those would silence a real difference for good. Instead they are checked again next
time — honest, and self-correcting.

The cost of that choice is that **the first run after turning the cache on is no faster than
before.** The benefit arrives on the second run. An export also warms it, so an
export-then-report cycle is warm already.

## 3. What invalidates it

`SyncStateCacheInvalidator` listens to Umbraco. There are three levels, and picking the right one
is the whole game:

| Level | When | Why |
|---|---|---|
| the item | saved, deleted, published, unpublished | only that item's own xml changed |
| the whole item type | moved, or moved to the recycle bin | a move rewrites the `Path` of every descendant, and paths are part of the serialized xml |
| **everything** | doc type, media type, member type, data type, template, language, or container saved/deleted/moved/renamed | these get embedded in *other* items' xml, so changing one silently changes items whose own rows never moved |

That last row is the important one. A content item's xml carries its doc type alias, template
alias, parent key and path; a dictionary item's carries its languages. So renaming a doc type
changes what every item using it serializes to, without touching those items or raising any
notification for them. There is no cheap way to work out which items are affected, and these
changes are rare, so we throw the whole cache away. The policy lives in one place —
`SyncStateCache`'s shared-item-types set — so it applies however the invalidation arrives.

Invalidation runs whether or not the setting is currently on. Otherwise turning the cache off,
editing things, and turning it back on would leave it confidently wrong.

The whole cache is also discarded when we cannot prove it still applies — see the identity
section below.

### Nothing pauses invalidation during uSync's own import

There is no `IsPaused` check in the invalidator, and that is deliberate. During an import, an item
uSync writes is invalidated and then simply not re-recorded (we only record confirmed `NoChange`
results), so invalidating is harmless — and it also catches items Umbraco saves as a *side effect*
of something we imported, which a paused check would miss.

## 4. Where it lives

`{IHostingEnvironment.LocalTempPath}/uSync/cache/state-{identity}.json`

**Never in the uSync folder.** The cache describes this site's database, not the source of truth,
so it must not travel between environments, get committed, or show up in a diff. (Same precedent
as [uSync.History](../../uSync.History/uSyncHistoryNotificationHandler.cs), which writes to
`LocalTempPath` too.)

### The identity

The file name and its contents both carry an identity hash. On load, a mismatch means the file is
discarded entirely. It is built from:

- **a database stamp** — a GUID we create once and keep in Umbraco's key/value table under
  `uSync.StateCache.Id` (the same pattern as
  [`SyncTrackerService`](../../uSync.BackOffice/Tracker/SyncTrackerService.cs)). Costs one
  key/value read per run, and catches a database restore, a database swap, or pointing the site at
  a different database. If we cannot read it, the cache is not used at all.
- **the uSync version** — a new version may serialize differently.
- **a fingerprint of the settings that affect serialization** — folders, root folder, folder mode,
  default extension, default set, and the default handler set's settings. Changing a handler
  setting changes the shape of the exported xml, so the recorded hashes stop meaning anything.

### The journal

Removing an entry from memory does nothing about the copy of it in the file on disk — and the file
is what the next restart trusts. But writing a whole manifest on every editor save would be far
too much.

So invalidations append a line to `state-{identity}.invalid` (a few bytes), and the journal is
replayed on load and folded back in whenever a fresh manifest is written. Both halves happen under
one lock, so an invalidation can never slip in between "manifest written" and "journal cleared"
and be lost.

## 5. What it cannot see

Read this section before turning it on. **With the cache on, uSync's change detection stops being
self-verifying and starts trusting its own bookkeeping.**

1. **A database change made by something that raises no Umbraco notification.** Raw SQL, a
   row-level restore, or an edit made by a differently configured instance. The database stamp
   catches a whole-database swap; it cannot catch a targeted edit. This is the accepted residual
   risk, and the reason the default is off.
2. **A cross-item dependency we have not thought of.** The known ones are handled (see section 3),
   but any serializer that pulls data in from another item is a new hole. **If you write a custom
   serializer that embeds data from a different item, that item's type needs adding to
   `SyncStateCache`'s shared-item-types set.**
3. **The report is no longer an independent check.** Report and import share one cache so they can
   never disagree with each other — but that means a wrong entry hides the item from the report
   too. A **force import** is the way to get a guaranteed full check; it bypasses the cache
   entirely.
4. **Dictionary items whose file key differs from the database key.** Dictionary items are matched
   by alias and uSync deliberately ignores the key when comparing them, but the cache files entries
   under the key in the file. If those two differ, an invalidation for the database item will not
   find the entry. Narrow, but real.
5. **Multiple handler sets with different serialization settings** run against the same folders.
   The identity fingerprint covers the default set only, so entries recorded under one set could be
   consulted under another. Leave the cache off if you do this.
6. **`LocalTempPath` is not guaranteed to survive.** Azure App Service recycles it, a container
   restart loses it, and on scale-out each instance has its own. All of those degrade to "slow
   first run", which is safe — but on some hosting the cache may rarely be warm and the feature
   will quietly do very little. Measure before promising anything.

Every one of these fails towards a *stale skip* at worst, and the cache is cleared by the next
relevant save. But a stale skip means an item that should have imported did not.

## 6. Operating it

- **Turn it on:** `"CacheImportState": true` under `uSync:Settings`. No restart needed — the
  setting is read at runtime.
- **Turn it off:** set it back to `false`. Behaviour and timings return to exactly what they were.
- **Clear it:** delete `{LocalTempPath}/uSync/cache/`, or run a force import (which ignores the
  cache for that run). Changing any handler setting also discards it.
- **See what it is doing:** at `Information` level, uSync logs `loaded {count} known items` on load
  and `state cache skipped {skipped} of {total} items` at the end of a run. If `skipped` is 0 on a
  second identical run, something is invalidating more than you expect.

## 7. Deliberately not done

**`UpdateDate` verification.** Storing each item's `UpdateDate` alongside the hash and validating
it against a single `IEntityService.GetAll(objectType)` query per handler would give existence plus
a timestamp for every item in one query, at no per-item cost. That would close limitation 1 above
for real Umbraco node types (not dictionary items, languages, domains or webhooks, which are not
in `umbracoNode`).

This is the obvious hardening step if notification-based invalidation proves too leaky in practice.
The manifest format has a version field so the extra data can be added without a migration.
