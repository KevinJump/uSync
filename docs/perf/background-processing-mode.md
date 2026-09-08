# Background processing mode - what it is, and the SQLite caveat

**Setting:** `uSync:Settings:ProcessingMode` — `Normal` (default) or `Background`.
Not enabled by default - opt in deliberately, and expect the SQLite caveat
below if you do.

## What it does

By default (`Normal`) uSync's report/import/export runs as a client-driven step
loop: the browser POSTs `usync/api/v1/Perform` once per handler, waiting for
each response before issuing the next. The user is tied to the uSync dashboard
for the whole run.

In `Background` mode, the whole run is handed to Umbraco's
`ILongRunningOperationService` as one continuous server-side operation
(`uSyncManagementService.PerformBackgroundActionAsync` /
`ProcessAllActions`). The initial request returns immediately with an
`operationId`; progress and completion arrive via SignalR (fast path) with a
polling fallback (`GET usync/api/v1/Status`) for when the socket isn't
connected — including reattaching after a page reload via
`GET usync/api/v1/Running`. The user is free to navigate elsewhere in the
backoffice while the sync runs.

`allowConcurrentExecution: false` is used per action type (`uSync:Report`,
`uSync:Import`, `uSync:Export`), so two background runs of the same action
can't run concurrently — a second attempt is rejected rather than silently
interleaved.

## Why the default is `Normal`

Moving to background processing removes the thing that, incidentally, kept
concurrent database access low during a run: in `Normal` mode the browser tab
is blocked awaiting each step, so it never generates other backoffice
traffic, and there are natural gaps between handler writes. In `Background`
mode the user (by design) goes and does other things — opens the content
tree, edits a page — while the import is still writing.

On SQL Server this is a non-issue (row/page locking, no single-writer
constraint). On **SQLite**, it is not:

- SQLite's default connection string for local/dev sites includes
  `Cache=Shared` (`SqliteDatabaseProviderMetadata.cs`), which enforces
  **table-level locks across connections**, not just file-level locks within
  one connection.
- A background import can hold a write lock on a table for an extended,
  continuous stretch (no inter-request gaps to release it), and that's true
  regardless of whether the concurrent reader goes through EF Core or NPoco —
  both ultimately hit the same SQLite database file.
- Any authenticated request during that window — not just uSync's own pages —
  needs an OpenIddict token-validation query against `UmbracoDbContext`
  before the request is even handled. If that table is locked, the query
  queues behind SQLite's writer and can exceed the EF command timeout (30s
  by default).

### Evidence

Reproduced against `uSyncSource.Site` (SQLite, `Cache=Shared`) on 2026-08-27.
A background import was started at `13:36:22` and had not logged "finished"
32 seconds later, when a concurrent request failed:

```
Failed executing DbCommand (30,144ms) ... RequestPath: /umbraco/usync/api/v1/Status
SQLite Error 6: 'database table is locked'
  SELECT ... FROM "umbracoOpenIddictTokens" ...
```

Error **6** is `SQLITE_LOCKED` (a shared-cache table-lock conflict), not error
**5** `SQLITE_BUSY` (plain file contention) — this distinction matters because
`busy_timeout` only retries `SQLITE_BUSY`; it does not help with
`SQLITE_LOCKED`. The failing request happened to be uSync's own status-poll
endpoint, whose fixed 2-second polling interval turned this from an
occasional risk into a reliable repro — the poll is gated to only hit the
server when SignalR isn't actually delivering, and backs off from 1s up to
20s while a run drags on, specifically to reduce this pressure. Note that on
SQLite, the closer starting interval trades a small amount of the original
mitigation for a progress bar that visibly updates during a run - it's still
gated behind the liveness check rather than being the sole progress
mechanism, but if you hit the contention below on SQLite, widening
`POLL_INTERVAL_MIN_MS` back up is the first thing to try. But the underlying
exposure isn't the poll: **any** backoffice request made while a large
background import is writing is at risk of the same failure. A user clicking
around the content tree during a multi-minute import can hit it too, just
less predictably.

## Guidance for anyone enabling `Background` mode

- On SQL Server, this is safe to enable — the risk described here is
  SQLite-specific.
- On SQLite, treat it as a real limitation, not a corner case: a large import
  can degrade or hard-fail other backoffice activity (by the same user or
  anyone else) for as long as it runs. Reducing exposure:
  - Removing `Cache=Shared` from the SQLite connection string trades shared-
    cache table locks for ordinary file-level locks, where `busy_timeout`
    *does* apply — this softens hard failures into brief waits, but does not
    eliminate contention on a large import.
  - Confirm WAL journal mode is actually active for the runtime connection
    (`SqliteDatabaseCreator` sets it at DB creation) — WAL lets readers
    proceed alongside a writer far better than the rollback-journal default.
  - Prefer running large first-imports/migrations during a maintenance
    window rather than expecting concurrent editorial use to be unaffected.
- If you hit `SQLite Error 6: 'database table is locked'` anywhere in the
  backoffice while a background sync is running, this is why — it is not
  data corruption, and the sync itself will generally still complete (or
  fail cleanly and report via the dashboard).

## What happens if the site crashes/restarts mid-run

Contention severe enough to fell the site (app pool recycle) leaves an
orphaned row in `umbracoLongRunningOperation` still marked `Running` — nothing
was left alive to move it to `Failed`. Two places read that row, with
different staleness behaviour:

- `GetByTypeAsync` (used to find/reattach to a run, and by Umbraco's own
  `allowConcurrentExecution: false` check) treats an Enqueued/Running row as
  gone once its `ExpirationDate` passes — so the dashboard, and the guard
  against starting a second concurrent run, both self-heal within Umbraco's
  `LongRunningOperations:ExpirationTime` (default **5 minutes**).
- `GetStatusAsync` (a single-operation lookup) returns the *raw* stored
  status with no staleness check — uSync's own status-poll endpoint used to
  call this directly, which meant a crashed run could report "Running"
  forever (in practice, until Umbraco's hourly cleanup job deletes the row).
  `uSyncManagementService.GetOperationStatusAsync` now cross-checks against
  the staleness-aware lookup and reports `Stale` once expired, matching the
  5-minute self-heal above.

So: after a crash, expect the "running in background" banner to clear on its
own within a few minutes, and a fresh run to be accepted once it does.

If you don't want to wait, or the banner still looks stuck: the banner has a
**"Not actually running? Reset this view"** button
(`uSyncWorkspaceContext.dismissBackgroundRun()`). This only clears what *this
browser tab* shows — it stops polling and unblocks the uSync dashboard
immediately. It cannot force Umbraco's own long-running-operation record to
end (there is no public cancel API for it), so if the server-side row hasn't
actually expired yet, a new run started right after dismissing may still be
briefly rejected as "already running" until the expiration window above
passes.

## Load-balanced backoffice

As of Umbraco 17, [load balancing the backoffice](https://docs.umbraco.com/umbraco-cms/17.latest/run-in-production/infrastructure-and-ops/server-setup/load-balancing/load-balancing-backoffice)
is an officially supported topology. uSync supports it, but **only in
`Background` mode.**

### Why `Normal` mode doesn't work

`Normal` mode drives a run as a sequence of client requests, each carrying a
`requestId` and `stepNumber`, with the accumulated results held in an
in-process cache on whichever server handled the previous step. If request
N+1 lands on a different server than request N — which is exactly what
happens under a round-robin load balancer with no sticky sessions — the run's
accumulated results are gone. There is no supported way to make this safe
without persisting the full result set between every step, which isn't worth
the cost for every non-load-balanced install. Use `Background` mode instead.

If you run `Normal` mode with more than one active backoffice server, uSync
logs a warning at startup — switch `uSync:Settings:ProcessingMode` to
`Background` to remove it.

### What `Background` mode needs, in addition to the above

- **The uSync folder on shared or replicated storage.** uSync reads and
  writes it (exports, imports, the uploaded-zip workflow) using physical
  paths — if each server has its own local copy, a file written on one server
  is invisible to the others.
- **`umbracoBuilder.LoadBalanceIsolatedCaches()`** in your composer, as
  required by Umbraco's own backoffice load-balancing docs — without it,
  repository caches are per-server and changes made by an import on one
  server won't be reflected on another until its cache expires.
- **SignalR is optional.** Progress and completion are pushed live over
  SignalR when it's working, and fall back to polling the `Status` endpoint
  when it isn't — including when the browser's socket is connected but to a
  *different* server than the one running the job, and so never receives
  anything for that run (the client detects this by tracking how long it's
  been since the last SignalR message, not just whether the socket is
  "connected"). A SignalR backplane (SQL Server, Redis) plus sticky sessions,
  or Azure SignalR, gives you live progress from any server; without either,
  uSync still works correctly, just via polling.
- **Progress reported cross-server is summary-only until the run
  completes.** A status request served by a different server than the one
  running the job reads the run's progress from the shared
  `umbracoLongRunningOperation` row rather than local memory — that row is
  written with just the handler summaries on every step (to keep each write
  small), and gains the full results list only on the write that completes
  the run.
- **A run still can't be cancelled**, and a **recycled app pool still loses
  it** (see above) — neither of those change under load balancing, they're
  just as true on a single server.
