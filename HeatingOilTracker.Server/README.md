# HeatingOilTracker.Server

Thin self-hosted replacement for the old Google Drive sync target. Holds one `TrackerData`
JSON blob and serves it over two endpoints, trusting the Tailscale tailnet as the only access
control (same model as `BookChat.Web` — see `KASSANDRA_SYNC_PLAN.md` at the repo root for the
full rationale).

## Endpoints

- `GET /api/tracker` — returns the stored document as a `SyncPayload` JSON body, or 404 if
  nothing has been pushed yet.
- `PUT /api/tracker` — accepts a `SyncPayload` JSON body, merges it against whatever is
  currently stored (same per-delivery / per-setting `ModifiedAt`-wins rule, 60-day tombstone
  prune, as `HeatingOilTracker.Core.Services.SyncMergeService`), persists the result, and
  returns the merged document.
- `GET /health` — trivial liveness check.

`WeatherHistory` and `BackupFolderPath` are never part of the wire payload — they stay
device-local, exactly as they did with the old Google Drive sync.

## Configuration

One environment variable:

- `TRACKER_DATA_PATH` — path to the JSON file the server persists to. Defaults to
  `./data/data.json` (relative to the working directory) when unset. In the container image
  this defaults to `/data/data.json`.

## Running locally

```powershell
dotnet run --project HeatingOilTracker.Server
```

Then from another terminal:

```powershell
curl http://localhost:5000/api/tracker
curl -X PUT http://localhost:5000/api/tracker -H "Content-Type: application/json" -d "{}"
```

(Port depends on the profile `dotnet run` picks — check the console output; override with
`ASPNETCORE_URLS` if needed.)

## Building the container image

Build context must be the **repository root** (the Dockerfile copies both
`HeatingOilTracker.Core` and `HeatingOilTracker.Server`):

```powershell
docker build -f HeatingOilTracker.Server/Dockerfile -t heatingoiltracker-sync .
```

## Deploying on kassandra (manual step — not done by this session)

This session has no SSH access to kassandra; the actual deploy is a manual step. Suggested
`docker-compose.yml` service, following the `bookchat-web` convention (bridge network with an
explicit host port, not `network_mode: host`):

```yaml
  heatingoiltracker-sync:
    build:
      context: ./heatingoiltracker  # working copy of this repo, same pattern as bookchat/src
      dockerfile: HeatingOilTracker.Server/Dockerfile
    container_name: heatingoiltracker-sync
    restart: unless-stopped
    ports:
      - "8086:8080"
    volumes:
      - ./heatingoiltracker/data:/data
    environment:
      - TRACKER_DATA_PATH=/data/data.json
```

Steps to run on kassandra:

1. Clone/copy this repo to `~/kassandra/heatingoiltracker` (mirroring how `~/kassandra/bookchat/src`
   holds a working copy of the BookChat repo).
2. Confirm port `8086` is free (next after BookChat's `8085`).
3. Add the service snippet above to `~/kassandra/docker-compose.yml`.
4. `docker compose up -d --build heatingoiltracker-sync`.
5. Verify from this machine and from the phone over Tailscale:
   `curl http://<kassandra-tailscale-ip>:8086/health`
6. Update `HeatingOilTracker.Maui/Secrets.cs`'s `KassandraSyncBaseUrl` (see
   `Secrets.template.cs`) to `http://<kassandra-tailscale-ip>:8086` and rebuild the MAUI app.
7. Update `KassandraWiki/Services/` with a new page for this service (image, purpose,
   networking, config dir, gotchas — same shape as `homepage.md`/`bookchat.md`), and add it to
   `access.md`'s service list. (Out of scope for this repo/session — do it in the wiki repo.)
