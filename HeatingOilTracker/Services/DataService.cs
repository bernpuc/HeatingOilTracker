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
}
