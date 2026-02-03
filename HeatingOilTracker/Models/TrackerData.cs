namespace HeatingOilTracker.Models;

public class TrackerData
{
    public decimal TankCapacityGallons { get; set; } = 275m;
    public List<OilDelivery> Deliveries { get; set; } = new();
    public Location Location { get; set; } = new();
    public List<DailyWeather> WeatherHistory { get; set; } = new();
    public ReminderSettings ReminderSettings { get; set; } = new();

    /// <summary>
    /// Path to folder where data backups are automatically saved.
    /// Set to a cloud-synced folder (OneDrive, Dropbox, etc.) for automatic cloud backup.
    /// </summary>
    public string? BackupFolderPath { get; set; }
}
