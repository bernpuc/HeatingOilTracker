namespace HeatingOilTracker.Core.Models;

public class TrackerData
{
    public decimal TankCapacityGallons { get; set; } = 275m;
    public List<OilDelivery> Deliveries { get; set; } = new();
    public Location Location { get; set; } = new();
    public List<DailyWeather> WeatherHistory { get; set; } = new();
    public ReminderSettings ReminderSettings { get; set; } = new();
    public RegionalSettings RegionalSettings { get; set; } = new();

    /// <summary>
    /// Path to folder where data backups are automatically saved.
    /// Set to a cloud-synced folder (OneDrive, Dropbox, etc.) for automatic cloud backup.
    /// </summary>
    public string? BackupFolderPath { get; set; }

    /// <summary>
    /// Per-setting modification timestamps. Tracked independently (rather than one
    /// combined "SettingsModifiedAt") so that syncing a change to one setting on a
    /// device doesn't drag along stale values for settings that haven't actually
    /// changed on that device.
    /// </summary>
    public DateTime TankCapacityModifiedAt { get; set; } = DateTime.MinValue;
    public DateTime LocationModifiedAt { get; set; } = DateTime.MinValue;
    public DateTime ReminderSettingsModifiedAt { get; set; } = DateTime.MinValue;
    public DateTime RegionalSettingsModifiedAt { get; set; } = DateTime.MinValue;
}
