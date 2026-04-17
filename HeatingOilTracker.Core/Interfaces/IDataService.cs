using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Core.Interfaces;

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
    /// <summary>
    /// Re-reads the latest local data from disk, unions the deliveries from
    /// <paramref name="syncResult"/> into it, applies remote settings if they
    /// are newer, then saves. Safe to call concurrently with Set*Async calls
    /// because it always starts from the freshest on-disk state.
    /// </summary>
    Task MergeFromSyncAsync(TrackerData syncResult);
    string GetDataFilePath();
    void InvalidateCache();
}
