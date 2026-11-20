# Startup import status

When uSync imports at startup (`uSync:Settings:ImportAtStartup`), it records the state of that
import in Umbraco's key-value store. A deployment pipeline can read it to wait for the import to
finish before it runs its own checks.

This matters mainly when the import runs in the background (`BackgroundStartup = true`, or
`ProcessingMode = Background`). Then the site starts serving requests while the import is still
running, so "the site is up" does not mean "the import is done".

## The endpoint

```
GET /umbraco/usync/api/v1/StartupStatus
```

It needs an authenticated backoffice user with access to the uSync tree, like the other uSync
endpoints. For a pipeline, create an Umbraco API user, give it a user group with access to the
uSync section, and get a token with the client credentials flow.

The response:

```json
{
  "state": "Completed",
  "startedUtc": "2026-11-19T10:15:02.123Z",
  "completedUtc": "2026-11-19T10:15:41.456Z",
  "itemsProcessed": 412,
  "changes": 17,
  "errors": 0,
  "isStale": false
}
```

`itemsProcessed`, `changes` and `errors` are only filled when the import finished.

## States

| State | Meaning |
| --- | --- |
| `None` | Nothing has been recorded (for example a new database). |
| `NotConfigured` | Import at startup is switched off. |
| `Blocked` | The stop file (`usync.stop`) blocked the import. |
| `Running` | The import is running. |
| `Completed` | The import finished without errors. |
| `CompletedWithErrors` | The import finished, but some items failed. `errors` says how many. |
| `Failed` | The import did not finish: an exception stopped it, or something before it (such as the startup export) failed. The site log has the details. |

When first boot has already run the same import in the same start up, the startup import is
recorded as `Completed` with zero counts.

## Polling from a pipeline

Only the latest record is kept, and uSync does not clear it when the site starts. Straight after a
deployment the endpoint can still return the record from the previous start up. So:

1. Note the time (UTC) just before you deploy.
2. Poll the endpoint until `startedUtc` is later than that time **and** `state` is no longer
   `Running`.
3. Treat `Completed` as success. Decide for yourself whether `CompletedWithErrors` fails the
   deployment. Treat `Failed` as a failure.

`NotConfigured` and `Blocked` have no `startedUtc`. If you expect either of them, check the state
rather than waiting for a new `startedUtc`.

On a load-balanced site only one server runs the startup import, but every server reads the same
record, so it does not matter which server answers the request.

## `isStale`

The `Running` record is written before the import starts, and replaced when it ends. If the server
stops in the middle (an app pool recycle, a crash), the record stays at `Running`.

uSync checks for that when you read the status: when the state is `Running` but there is no active
`uSync:Startup` long-running operation any more, `isStale` is `true`. That means the run was
abandoned and will not finish. Umbraco expires a long-running operation a few minutes after its
server stops renewing it (`LongRunningOperations:ExpirationTime`, five minutes by default), so
`isStale` can take that long to turn `true`.
