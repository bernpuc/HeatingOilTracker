using HeatingOilTracker.Core.Models;
using HeatingOilTracker.Core.Services;
using HeatingOilTracker.Server;

var builder = WebApplication.CreateBuilder(args);

// Trusted-network-only service (Tailscale is the access boundary, see KASSANDRA_SYNC_PLAN.md) —
// there's no app-level auth. The one concession to a hostile payload is a small body-size cap,
// since a TrackerData blob for one household tank is always tiny.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024; // 5 MB
});

var dataPath = Environment.GetEnvironmentVariable("TRACKER_DATA_PATH") ?? "./data/data.json";
builder.Services.AddSingleton(new TrackerStore(dataPath));
builder.Services.AddSingleton<SyncMergeService>();

var app = builder.Build();

app.MapGet("/api/tracker", async (TrackerStore store) =>
{
    var found = await store.WithLockAsync(current => Task.FromResult(current));
    return found is null ? Results.NotFound() : Results.Ok(SyncPayload.FromTrackerData(found));
});

app.MapPut("/api/tracker", async (SyncPayload incoming, TrackerStore store, SyncMergeService merge) =>
{
    var merged = await store.WithLockAsync(async current =>
    {
        // "local" / "remote" here just means: incoming push vs. whatever was already stored.
        // Merge is commutative except for ties (equal ModifiedAt), where the first argument
        // wins — picking the incoming write for ties is the more intuitive "last write wins".
        var result = current is null
            ? incoming.ToTrackerData()
            : merge.Merge(incoming.ToTrackerData(), current);

        await store.SaveAsync(result);
        return result;
    });

    return Results.Ok(SyncPayload.FromTrackerData(merged));
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
