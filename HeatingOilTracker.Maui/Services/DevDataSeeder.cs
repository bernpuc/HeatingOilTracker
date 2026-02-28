using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Maui.Services;

public static class DevDataSeeder
{
    public static async Task SeedIfEmptyAsync(IDataService dataService)
    {
        var deliveries = await dataService.GetDeliveriesAsync();
        if (deliveries.Count > 0) return; // already has data

        // Seed some realistic deliveries
        var testDeliveries = new List<OilDelivery>
        {
            new OilDelivery
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-180),
                Gallons = 150m,
                PricePerGallon = 3.89m,
                FilledToCapacity = true,
                Notes = "Fall fill-up"
            },
            new OilDelivery
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-90),
                Gallons = 120m,
                PricePerGallon = 3.75m,
                FilledToCapacity = false,
                Notes = "Winter top-off"
            },
            new OilDelivery
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-30),
                Gallons = 100m,
                PricePerGallon = 3.95m,
                FilledToCapacity = false,
                Notes = "Recent delivery"
            }
        };

        foreach (var delivery in testDeliveries)
            await dataService.AddDeliveryAsync(delivery);

        // Seed tank capacity
        await dataService.SetTankCapacityAsync(275m);

        // Seed location (Connecticut since that's where you are)
        await dataService.SetLocationAsync(new Core.Models.Location
        {
            DisplayName = "Naugatuck, Connecticut, US",
            Latitude = 41.4859m,
            Longitude = -73.0507m
        });
    }
}