# Investigation: batched content/media save on import (perf item #1)

**Branch:** `v17/investigate/batch-save`
**Question:** would routing content/media imports through Umbraco's
`Save(IEnumerable<…>)` (the batch overload) meaningfully reduce database work,
given that (a) Umbraco's own batching is limited and (b) notifications may not be
batched?

**Short answer:** No — not as a safe, general win. In the default configuration
the only saving would come at the cost of the per-item failure isolation that the
current default is deliberately designed to provide. In the opt-in "suppressed"
configuration the transaction and notifications are *already* batched by uSync's
ambient scope, so the batch overload adds almost nothing. Recommendation: **do not
wire up bulk `Save` for content/media.** Details and evidence below.

---

## 1. What the batch overload actually does

Decompiled from `Umbraco.Cms.Core.Services.ContentService` (Umbraco 17.3.0,
`Umbraco.Core.dll`). Media (`MediaService`) is equivalent.

`Save(IContent)` — the per-item path uSync uses today:

```csharp
using (ICoreScope scope = ScopeProvider.CreateCoreScope())
{
    scope.WriteLock(Constants.Locks.ContentTree);
    if (scope.Notifications.PublishCancelable(new ContentSavingNotification(content, …)))
        { scope.Complete(); return Cancel; }
    _documentRepository.Save(content);                 // 1 row write
    scope.Notifications.Publish(new ContentSavedNotification(content, …));
    scope.Notifications.Publish(new ContentTreeChangeNotification(content, RefreshNode, …));
    Audit(...);
    scope.Complete();                                   // 1 transaction commit
}
```

`Save(IEnumerable<IContent>)` — the batch overload:

```csharp
IContent[] array = contents.ToArray();
using (ICoreScope scope = ScopeProvider.CreateCoreScope())
{
    scope.WriteLock(Constants.Locks.ContentTree);
    if (scope.Notifications.PublishCancelable(new ContentSavingNotification(array, …)))  // ONE, batched
        { scope.Complete(); return Cancel; }
    foreach (IContent content in array)
        _documentRepository.Save(content);              // still 1 row write PER item
    scope.Notifications.Publish(new ContentSavedNotification(array, …));                 // ONE, batched
    scope.Notifications.Publish(new ContentTreeChangeNotification(array, RefreshNode, …));// ONE, batched
    Audit(...);
    scope.Complete();                                   // ONE transaction commit
}
```

Key observations:

- **The actual row writes are identical** — `_documentRepository.Save(content)` runs
  once per item in both. The batch overload does **not** issue a single set-based
  SQL statement; it loops. So there is **no reduction in the number of INSERT/UPDATE
  round-trips**.
- The batch overload's savings are purely **structural**: 1 scope/transaction/commit
  and 1 write-lock instead of N, and **notifications *are* batched** — `ContentSaving`,
  `ContentSaved` and `ContentTreeChange` each fire **once with an array** rather than
  N times. (This corrects the assumption that "notifications aren't batched" — at the
  `ContentService` level they are, when you use the batch overload.)
- The batch overload also **skips** the per-item validation that `Save(IContent)`
  performs: the `PublishedState` guard and the 255-char name-length check. It also
  doesn't accept a `ContentSchedule`.

So the theoretical benefit of switching is: **N transactions → 1, and N notification
dispatches → 1.** No change to the number of row writes.

## 2. Does uSync actually pay "N transactions" today? It depends on config.

uSync wraps an import handler run in
`ICoreScopeProvider.CreateNotificationScope(...)`
([`ScopeExtensions.cs`](../../uSync.BackOffice/Extensions/ScopeExtensions.cs)):

```csharp
if (syncConfigService.Settings.DisableNotificationSuppression)
    return null;                                    // <-- default path
return scopeProvider.CreateCoreScope(
    scopedNotificationPublisher: notificationPublisher,   // SyncScopedNotificationPublisher
    autoComplete: true);
```

`DisableNotificationSuppression` **defaults to `true`** on v16+
([`uSyncSettings.cs:224`](../../uSync.BackOffice/Configuration/uSyncSettings.cs)),
so there are two very different runtime shapes:

### Default config — `DisableNotificationSuppression = true`

`CreateNotificationScope` returns **null**. There is **no uSync ambient scope**
around the import (`SyncService_Handlers.cs`: `scope?.Complete()` is a no-op). Each
per-item `contentService.Save(item)` therefore opens its **own** root scope →
its **own** transaction/commit, and fires its notifications **immediately**.

- Here, N items really do mean **N transactions + N notification sets**.
- The batch overload *would* collapse these to 1 + 1.
- **BUT** this per-item, non-batched behaviour is a *deliberate design decision*.
  From the setting's own XML docs:

  > on v16 the default is true, because some of the notifications appear to be
  > closely coupled to the save/publish process, and if something goes wrong in one
  > item's import it can cause a cascade of failures across everything that might have
  > been imported along with it. If the notifications are not suppressed, then if an
  > item fails to import it doesn't stop other items from being imported.

  Batch-saving reintroduces exactly the failure mode this default exists to avoid:
  a single bad item (DB constraint, a throwing `Saving`/`Saved` handler, an
  over-long name that the batch overload no longer validates) rolls back or aborts
  the **whole batch**, and uSync loses its per-item error attribution (which item
  failed, with what message). This matches the "batching causes issues" experience.

### Opt-in config — `DisableNotificationSuppression = false`

`CreateNotificationScope` returns a real ambient scope using
`SyncScopedNotificationPublisher`
([`SyncScopedNotificationPublisher.cs`](../../uSync.BackOffice/Notifications/SyncScopedNotificationPublisher.cs)).

- **Transaction is already batched.** Umbraco scopes nest; the per-item
  `contentService.Save` calls become child scopes that share the single ambient
  transaction, which commits once when uSync completes the outer scope. So the
  "N transactions" cost is **already gone** without the batch overload.
- **Notifications are already deferred and grouped.** The scoped publisher collects
  every notification raised during the import and, at completion, dispatches them
  grouped by type in one `IEventAggregator.Publish(items)` call per type — or, if
  `BackgroundNotifications = true`, hands them to the background task queue.

  In this mode, switching to the batch overload only changes "N single-entity
  `ContentSavedNotification`s, then group-published" into "1 array
  `ContentSavedNotification`". That is a marginal allocation/dispatch difference,
  **not** a database saving.

## 3. Why the schema types already use the bulk path — and content doesn't

This asymmetry is intentional. `ContentTypeSerializer`, `MediaTypeSerializer`,
`MemberTypeSerializer` and `DataTypeSerializer` already override `SaveAsync` and
honour `SerializerFlags.DoNotSave`, because saving a **doctype/datatype** triggers
expensive schema changes and full cache/nucache rebuilds — there, batching many into
one operation is a genuine, large win and the items are few. For **content/media**
items the per-save cost is dominated by the unavoidable per-row write, and the items
are many, so the batch overload buys far less while adding the atomicity risk above.

The handler-level bulk hook does exist —
[`SyncHandlerRoot.ImportAllAsync`](../../uSync.BackOffice/SyncHandlers/SyncHandlerRoot.cs)
calls `serializer.SaveAsync(updates…)` — but only under
`if (options.Flags.HasFlag(SerializerFlags.DoNotSave))`, and `DoNotSave` is never set
for the content/media import path. So the plumbing is present but deliberately dormant
for these types.

## 4. Conclusion & recommendation

| Config | Transactions today | Notifications today | Batch overload benefit | Cost/risk |
|---|---|---|---|---|
| **Default** (`DisableNotificationSuppression = true`) | N (one per item) | N, fired immediately | N→1 txn, N→1 notifications | **Loses per-item failure isolation** (the reason this is the default); no reduction in row writes |
| **Suppressed** (`= false`) | 1 (ambient scope) | Deferred + grouped (opt. background) | ~none (already batched) | Marginal |

**Recommendation: do not wire up bulk `Save(IEnumerable)` for content/media.**
It delivers no reduction in the dominant cost (per-row writes), its transaction/
notification batching is either already provided by the suppressed-scope path or
directly conflicts with the intentional per-item isolation of the default path, and
it removes per-item error reporting plus two validation checks.

If import throughput on large content sets is the goal, the existing, lower-risk
levers are configuration, not code:

- **`DisableNotificationSuppression = false`** — collapses the import to a single
  transaction and defers/groups notifications (this is the "batch" people actually
  want, done at the scope level rather than the save level).
- **`BackgroundNotifications = true`** — moves notification processing off the import
  thread entirely.

If a save-level optimisation is ever revisited, the only shape worth prototyping is a
**bounded** batch (e.g. save in chunks of N with a per-chunk try/catch that falls back
to per-item on failure) so the failure blast radius stays small — and it should be
measured against a real content tree before adoption, since the row-write cost (which
batching does not change) is expected to dominate.
