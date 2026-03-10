using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Core.Services;

public class SyncMergeService
{
    private const int TombstonePruneDays = 60;

    public TrackerData Merge(TrackerData local, TrackerData remote)
    {
        // 1. Union deliveries by GUID; winner = higher ModifiedAt
        //    (IsDeleted=true with newer ModifiedAt beats IsDeleted=false)
        var merged = local.Deliveries
            .Concat(remote.Deliveries)
            .GroupBy(d => d.Id)
            .Select(g => g.OrderByDescending(d => d.ModifiedAt).First())
            .ToList();

        // 2. Prune stale tombstones (both devices have seen them by now)
        var pruneDate = DateTime.UtcNow.AddDays(-TombstonePruneDays);
        merged = merged
            .Where(d => !d.IsDeleted || d.ModifiedAt > pruneDate)
            .ToList();

        // 3. Settings: take whichever side modified them more recently
        bool useRemoteSettings = remote.SettingsModifiedAt > local.SettingsModifiedAt;

        return new TrackerData
        {
            Deliveries = merged,
            TankCapacityGallons = useRemoteSettings ? remote.TankCapacityGallons : local.TankCapacityGallons,
            Location = useRemoteSettings ? remote.Location : local.Location,
            ReminderSettings = useRemoteSettings ? remote.ReminderSettings : local.ReminderSettings,
            RegionalSettings = useRemoteSettings ? remote.RegionalSettings : local.RegionalSettings,
            SettingsModifiedAt = useRemoteSettings ? remote.SettingsModifiedAt : local.SettingsModifiedAt,
            // Always keep local-only fields:
            WeatherHistory = local.WeatherHistory,
            BackupFolderPath = local.BackupFolderPath,
        };
    }
}
