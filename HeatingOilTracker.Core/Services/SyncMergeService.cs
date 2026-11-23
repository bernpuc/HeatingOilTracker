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

        // 3. Settings: merge each one independently by its own modification time,
        //    so a change to one setting doesn't drag along stale values for
        //    settings that haven't actually changed on that side.
        bool useRemoteTankCapacity = remote.TankCapacityModifiedAt > local.TankCapacityModifiedAt;
        bool useRemoteLocation = remote.LocationModifiedAt > local.LocationModifiedAt;
        bool useRemoteReminderSettings = remote.ReminderSettingsModifiedAt > local.ReminderSettingsModifiedAt;
        bool useRemoteRegionalSettings = remote.RegionalSettingsModifiedAt > local.RegionalSettingsModifiedAt;

        return new TrackerData
        {
            Deliveries = merged,
            TankCapacityGallons = useRemoteTankCapacity ? remote.TankCapacityGallons : local.TankCapacityGallons,
            TankCapacityModifiedAt = useRemoteTankCapacity ? remote.TankCapacityModifiedAt : local.TankCapacityModifiedAt,
            Location = useRemoteLocation ? remote.Location : local.Location,
            LocationModifiedAt = useRemoteLocation ? remote.LocationModifiedAt : local.LocationModifiedAt,
            ReminderSettings = useRemoteReminderSettings ? remote.ReminderSettings : local.ReminderSettings,
            ReminderSettingsModifiedAt = useRemoteReminderSettings ? remote.ReminderSettingsModifiedAt : local.ReminderSettingsModifiedAt,
            RegionalSettings = useRemoteRegionalSettings ? remote.RegionalSettings : local.RegionalSettings,
            RegionalSettingsModifiedAt = useRemoteRegionalSettings ? remote.RegionalSettingsModifiedAt : local.RegionalSettingsModifiedAt,
            // Always keep local-only fields:
            WeatherHistory = local.WeatherHistory,
            BackupFolderPath = local.BackupFolderPath,
        };
    }
}
