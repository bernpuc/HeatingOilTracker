# Plan: Replace Google Drive Sync with Kassandra-Hosted Sync

## Why

`GoogleDriveSyncService` (`HeatingOilTracker.Maui/Services/GoogleDriveSyncService.cs`) syncs
`TrackerData` (minus `WeatherHistory`, which stays device-local) between the Windows and Android
builds via OAuth2 PKCE against Google Drive's hidden `appDataFolder`. That buys almost nothing here:

- Both devices are reachable over Tailscale already (same pattern BookChat.Web already uses —
  no app-level login, access controlled entirely by tailnet membership).
- The home server `kassandra` is always on, so a thin HTTP sync target is now trivial to host.
- The OAuth machinery (Google Cloud Console project, two separate OAuth client types —
  Desktop for Windows with a loopback listener + client secret, Android via `WebAuthenticator` +
  custom URI scheme — PKCE, refresh-token storage in `SecureStorage`, `Secrets.cs`/
  `Secrets.template.cs`) is pure overhead for moving one small JSON blob between two devices that
  can already reach each other directly.
- Replacing a Drive-backed blob with a server-authoritative store also removes the need for
  `SyncMergeService`'s last-write-wins per-field merge — there's only one copy of the data once
  both clients talk to the same server.

## What stays exactly as-is

- `IDataService` / `DataService` — local JSON cache at `%APPDATA%\HeatingOilTracker\data.json`,
  atomic write, optional `BackupFolderPath` cloud-folder copy. Untouched. The new sync service
  still plugs into the existing `_syncService?.PushAsync(data)` fire-and-forget hook in
  `DataService.SaveAsync`.
- `WeatherService` / `EiaService` — never synced via Google, stay per-device, no change.
- `TrackerData`, `OilDelivery.ModifiedAt`/`IsDeleted` tombstone fields — reused as-is; the server
  store is keyed the same way.
- The MAUI UI layer, `ITankEstimatorService`, `IReportService`, `ICsvImportService` — untouched.

## What changes

### 1. New `kassandra` Docker service: `heatingoiltracker-sync`

A minimal ASP.NET Core 9 Web API, added to `~/kassandra/docker-compose.yml` following the stack's
existing conventions (see `KassandraWiki/Network/access.md` / `Services/*.md` on the admin
machine — not duplicated here): `network_mode: host` isn't required since nothing else needs to
reach it by Compose DNS name; a normal bridge mapping on a free port (e.g. `8086`) is enough,
matching the `bookchat-web` pattern (bridge + explicit host port) rather than the host-networked
majority.

- Single responsibility: hold one `TrackerData` JSON blob (reusing the exact same shape
  `DataService` already serializes) and serve it over a tiny REST surface.
- Storage: a single `data.json` file on a bind-mounted volume under `~/kassandra/heatingoiltracker/data`
  (tracked the same way other service data dirs are — i.e. *not* committed to `kassandra-config`,
  per that repo's existing `*/config/` exclusion rule).
- No authentication layer of its own. Trust boundary is Tailscale: only tailnet members (today,
  `kassandra` and `bernards-s22`) can reach the host/port at all. This mirrors BookChat.Web's
  existing model exactly — nothing new to design here, just reuse the precedent.
- Endpoints (deliberately tiny — this is a single-document store, not a general sync protocol):
  - `GET /api/tracker` → returns the current stored `TrackerData` JSON (or 404 if none exists yet).
  - `PUT /api/tracker` → accepts a full `TrackerData` JSON body, merges it against the
    currently-stored copy using the **same merge rule already in `SyncMergeService`**
    (per-delivery `ModifiedAt` wins, per-setting independent `ModifiedAt` wins, 60-day tombstone
    prune), persists the merged result, and returns the merged document back to the caller.
  - Doing the merge server-side (not client-side) is what actually eliminates `SyncMergeService`
    from the client: the server becomes the single arbiter, and both devices just push their
    local view and take back whatever the server decides is authoritative.
- `WeatherHistory` and `BackupFolderPath` excluded from the wire payload, same as today's
  `SyncPayload` DTO in `GoogleDriveSyncService` — copy that exclusion list verbatim.

### 2. New project: `HeatingOilTracker.Server`

A new minimal-API project added to `HeatingOilTracker.sln`, living in this repo (not a copy
pasted onto kassandra by hand) so it's built and versioned the same way as everything else, then
containerized for deployment — same shape as `BookChat.Web`'s relationship to the `BookChat` repo
(`~/kassandra/bookchat/src` is a working copy of that repo, rebuilt via its own Dockerfile).

- References `HeatingOilTracker.Core` directly so it can reuse `TrackerData`, the merge logic
  lifted from `SyncMergeService` (move it into `HeatingOilTracker.Core.Services` if it isn't
  already visible to a new project — it already lives in Core, so no move needed), and the
  existing `System.Text.Json` options/camelCase convention `DataService` uses.
- A `Dockerfile` (multi-stage `dotnet publish`) alongside it, plus a `docker-compose.yml` snippet
  to hand to the kassandra session for the actual deploy.

### 3. New client: `HeatingOilTracker.Maui/Services/KassandraSyncService.cs`

Implements the existing `ISyncService` interface unchanged:

```csharp
Task<SyncResult> SyncOnStartupAsync(TrackerData localData);
Task PushAsync(TrackerData data);
Task<bool> SignInAsync();
Task SignOutAsync();
bool IsConfigured { get; }
bool IsSignedIn { get; }
string? AccountEmail { get; }
DateTime? LastSyncAt { get; }
```

- `SyncOnStartupAsync` → `PUT /api/tracker` with local data, store the merged response via
  `IDataService.MergeFromSyncAsync` (already exists, already does the right "re-load fresh local
  state before merging" dance — untouched).
- `PushAsync` → same `PUT`, fire-and-forget, matching today's behavior.
- `SignInAsync`/`SignOutAsync`/`IsSignedIn`/`AccountEmail` → no real auth exists, so these either
  collapse to trivial no-ops (`IsSignedIn` always `true`, `SignInAsync` always returns `true`
  immediately) or get re-purposed as a simple "is the kassandra endpoint reachable right now"
  check. Needs a decision during implementation — see Open Questions.
- The whole OAuth surface (`Secrets.cs`/`Secrets.template.cs`, PKCE, `HttpListener` loopback flow,
  `WebAuthenticator`, `SecureStorage` token keys) is deleted, not kept behind a flag — there's no
  reason to maintain a disabled Google path once this ships.
- Base URL: configurable, pointing at kassandra's Tailscale IP or `kassandra.local` depending on
  network (same dual-URL pattern already used for Home Assistant's External/Internal URL in the
  HA companion app) — likely just one setting since, unlike HA, this is a background sync call,
  not something the user navigates to directly. A straightforward "use `kassandra.local:8086` on
  LAN, fall back to the Tailscale IP otherwise" or just always use the Tailscale IP (works on LAN
  too) — simplest to just hardcode the Tailscale IP given this is a single-user homelab app, not a
  distributed product config.

### 4. Delete, once the above is verified working

- `HeatingOilTracker.Maui/Services/GoogleDriveSyncService.cs`
- `HeatingOilTracker.Core/Services/SyncMergeService.cs` *or* repurpose its merge method
  server-side (see above) — either way it stops being a client-side dependency.
- `HeatingOilTracker.Tests/Services/SyncMergeServiceTests.cs` → becomes a test of the server's
  merge endpoint/logic instead (same assertions, moved to wherever the merge code now lives).
- Any `Secrets.cs`/`Secrets.template.cs` Google client ID/secret scaffolding and related
  `.gitignore` entries.
- `MauiProgram.cs` DI registration swaps `GoogleDriveSyncService` for `KassandraSyncService`.

## Sequencing

1. `HeatingOilTracker.Server` project + Dockerfile, built and tested locally first (`dotnet run`,
   hit it with `curl` from this machine) before touching kassandra at all.
2. Deploy to kassandra: add the compose service, verify `GET/PUT /api/tracker` reachable at
   `http://<tailscale-ip>:8086` from both this machine and, ideally, the phone over cellular
   (mirrors the existing BookChat Web Tailscale verification already done).
3. `KassandraSyncService` in the MAUI app, wired in behind the existing `ISyncService`
   abstraction — this is the step that needs zero UI changes, since the interface doesn't change.
4. Manual two-device verification: a delivery added on Windows shows up on Android and vice versa,
   a tombstoned delivery doesn't resurrect, settings merge correctly.
5. Remove the Google Drive sync path and its OAuth scaffolding.
6. Update `KassandraWiki/Services/` with a new page for the service, same shape as the existing
   `homepage.md`/`bookchat.md` pages (image, purpose, networking, config dir, gotchas) — and
   `access.md`'s service list.

## Open Questions (resolve during implementation, not blocking the plan)

- Does `ISyncService.SignInAsync`/`IsSignedIn` get gutted to trivial always-true no-ops, or
  repurposed as a lightweight reachability/health check surfaced in Settings (e.g. "Last synced
  with kassandra: 2 minutes ago" vs. today's "Signed in as x@gmail.com")? Affects the Settings
  page's sync status UI, which isn't otherwise touched by this plan.
- Exact free port on kassandra for the new service (suggest `8086`, next after BookChat's `8085`
  — confirm nothing else already uses it).
- Whether the merge endpoint needs any request size/rate limiting — almost certainly not, given
  this is two trusted devices on a private tailnet syncing a tiny JSON document, but worth a
  one-line note in the server project rather than silent omission.
