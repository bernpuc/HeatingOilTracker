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
    string GetDataFilePath();
}
