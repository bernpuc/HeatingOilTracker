using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public interface IDataService
{
    Task<TrackerData> LoadAsync();
    Task SaveAsync(TrackerData data);
    Task<List<OilDelivery>> GetDeliveriesAsync();
    Task AddDeliveryAsync(OilDelivery delivery);
    Task UpdateDeliveryAsync(OilDelivery delivery);
    Task DeleteDeliveryAsync(Guid id);
    Task<decimal> GetTankCapacityAsync();
    Task SetTankCapacityAsync(decimal capacity);
    Task<Location> GetLocationAsync();
    Task SetLocationAsync(Location location);
    Task<List<DailyWeather>> GetWeatherHistoryAsync();
    Task AddWeatherDataAsync(List<DailyWeather> weatherData);
    Task<ReminderSettings> GetReminderSettingsAsync();
    Task SetReminderSettingsAsync(ReminderSettings settings);
    Task<string?> GetBackupFolderPathAsync();
    Task SetBackupFolderPathAsync(string? path);
    Task<RegionalSettings> GetRegionalSettingsAsync();
    Task SetRegionalSettingsAsync(RegionalSettings settings);
    string GetDataFilePath();
}
