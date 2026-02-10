using HeatingOilTracker.Models;
using System.IO;
using System.Text.Json;

namespace HeatingOilTracker.Services;

public class DataService : IDataService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HeatingOilTracker");

    private static readonly string DataFilePath = Path.Combine(DataDirectory, "data.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private TrackerData? _cachedData;

    public string GetDataFilePath() => DataFilePath;

    public async Task<TrackerData> LoadAsync()
    {
        if (_cachedData != null)
            return _cachedData;

        if (!File.Exists(DataFilePath))
        {
            _cachedData = new TrackerData();
            return _cachedData;
        }

        var json = await File.ReadAllTextAsync(DataFilePath);
        _cachedData = JsonSerializer.Deserialize<TrackerData>(json, JsonOptions) ?? new TrackerData();
        return _cachedData;
    }

    public async Task SaveAsync(TrackerData data)
    {
        if (!Directory.Exists(DataDirectory))
            Directory.CreateDirectory(DataDirectory);

        var json = JsonSerializer.Serialize(data, JsonOptions);

        // Atomic write: write to temp file then rename
        var tempPath = DataFilePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json);

        if (File.Exists(DataFilePath))
            File.Delete(DataFilePath);

        File.Move(tempPath, DataFilePath);
        _cachedData = data;

        // Auto-backup to configured folder if set
        await BackupToCloudFolderAsync(data);
    }

    private async Task BackupToCloudFolderAsync(TrackerData data)
    {
        if (string.IsNullOrWhiteSpace(data.BackupFolderPath))
            return;

        try
        {
            if (!Directory.Exists(data.BackupFolderPath))
                Directory.CreateDirectory(data.BackupFolderPath);

            var backupFileName = "HeatingOilTracker_backup.json";
            var backupPath = Path.Combine(data.BackupFolderPath, backupFileName);
            var json = JsonSerializer.Serialize(data, JsonOptions);

            // Atomic write to backup location
            var tempBackupPath = backupPath + ".tmp";
            await File.WriteAllTextAsync(tempBackupPath, json);

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Move(tempBackupPath, backupPath);
        }
        catch (Exception ex)
        {
            // Log but don't fail the main save operation
            System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
        }
    }

    public async Task<List<OilDelivery>> GetDeliveriesAsync()
    {
        var data = await LoadAsync();
        return data.Deliveries.OrderByDescending(d => d.Date).ToList();
    }

    public async Task AddDeliveryAsync(OilDelivery delivery)
    {
        var data = await LoadAsync();
        data.Deliveries.Add(delivery);
        await SaveAsync(data);
    }

    public async Task UpdateDeliveryAsync(OilDelivery delivery)
    {
        var data = await LoadAsync();
        var index = data.Deliveries.FindIndex(d => d.Id == delivery.Id);
        if (index >= 0)
        {
            data.Deliveries[index] = delivery;
            await SaveAsync(data);
        }
    }

    public async Task DeleteDeliveryAsync(Guid id)
    {
        var data = await LoadAsync();
        data.Deliveries.RemoveAll(d => d.Id == id);
        await SaveAsync(data);
    }

    public async Task<decimal> GetTankCapacityAsync()
    {
        var data = await LoadAsync();
        return data.TankCapacityGallons;
    }

    public async Task SetTankCapacityAsync(decimal capacity)
    {
        var data = await LoadAsync();
        data.TankCapacityGallons = capacity;
        await SaveAsync(data);
    }

    public async Task<Location> GetLocationAsync()
    {
        var data = await LoadAsync();
        return data.Location;
    }

    public async Task SetLocationAsync(Location location)
    {
        var data = await LoadAsync();
        data.Location = location;
        await SaveAsync(data);
    }

    public async Task<List<DailyWeather>> GetWeatherHistoryAsync()
    {
        var data = await LoadAsync();
        return data.WeatherHistory;
    }

    public async Task AddWeatherDataAsync(List<DailyWeather> weatherData)
    {
        var data = await LoadAsync();

        // Merge with existing, avoiding duplicates by date
        var existingDates = data.WeatherHistory.Select(w => w.Date.Date).ToHashSet();
        var newData = weatherData.Where(w => !existingDates.Contains(w.Date.Date));
        data.WeatherHistory.AddRange(newData);

        // Keep sorted by date
        data.WeatherHistory = data.WeatherHistory.OrderBy(w => w.Date).ToList();

        await SaveAsync(data);
    }

    public async Task<ReminderSettings> GetReminderSettingsAsync()
    {
        var data = await LoadAsync();
        return data.ReminderSettings;
    }

    public async Task SetReminderSettingsAsync(ReminderSettings settings)
    {
        var data = await LoadAsync();
        data.ReminderSettings = settings;
        await SaveAsync(data);
    }

    public async Task<string?> GetBackupFolderPathAsync()
    {
        var data = await LoadAsync();
        return data.BackupFolderPath;
    }

    public async Task SetBackupFolderPathAsync(string? path)
    {
        var data = await LoadAsync();
        data.BackupFolderPath = path;
        await SaveAsync(data);
    }

    public async Task<RegionalSettings> GetRegionalSettingsAsync()
    {
        var data = await LoadAsync();
        return data.RegionalSettings;
    }

    public async Task SetRegionalSettingsAsync(RegionalSettings settings)
    {
        var data = await LoadAsync();
        data.RegionalSettings = settings;
        await SaveAsync(data);
    }
}
