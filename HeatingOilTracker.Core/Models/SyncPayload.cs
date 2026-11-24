namespace HeatingOilTracker.Core.Models;

/// <summary>
/// Wire shape for TrackerData sync (client &lt;-&gt; HeatingOilTracker.Server, and previously
/// client &lt;-&gt; Google Drive). Deliberately excludes WeatherHistory and BackupFolderPath,
/// which stay device-local and never travel over the wire.
/// </summary>
public class SyncPayload
{
    public decimal TankCapacityGallons { get; set; } = 275m;
    public List<OilDelivery> Deliveries { get; set; } = new();
    public Location Location { get; set; } = new();
    public ReminderSettings ReminderSettings { get; set; } = new();
    public RegionalSettings RegionalSettings { get; set; } = new();
    public DateTime TankCapacityModifiedAt { get; set; }
    public DateTime LocationModifiedAt { get; set; }
    public DateTime ReminderSettingsModifiedAt { get; set; }
    public DateTime RegionalSettingsModifiedAt { get; set; }

    public static SyncPayload FromTrackerData(TrackerData data) => new()
    {
        TankCapacityGallons = data.TankCapacityGallons,
        Deliveries = data.Deliveries,
        Location = data.Location,
        ReminderSettings = data.ReminderSettings,
        RegionalSettings = data.RegionalSettings,
        TankCapacityModifiedAt = data.TankCapacityModifiedAt,
        LocationModifiedAt = data.LocationModifiedAt,
        ReminderSettingsModifiedAt = data.ReminderSettingsModifiedAt,
        RegionalSettingsModifiedAt = data.RegionalSettingsModifiedAt
    };

    /// <summary>
    /// Expands back into a full TrackerData. WeatherHistory and BackupFolderPath are left at
    /// their defaults (empty/null) — the caller fills those in from local state when needed.
    /// </summary>
    public TrackerData ToTrackerData() => new()
    {
        TankCapacityGallons = TankCapacityGallons,
        Deliveries = Deliveries,
        Location = Location,
        ReminderSettings = ReminderSettings,
        RegionalSettings = RegionalSettings,
        TankCapacityModifiedAt = TankCapacityModifiedAt,
        LocationModifiedAt = LocationModifiedAt,
        ReminderSettingsModifiedAt = ReminderSettingsModifiedAt,
        RegionalSettingsModifiedAt = RegionalSettingsModifiedAt
    };
}
