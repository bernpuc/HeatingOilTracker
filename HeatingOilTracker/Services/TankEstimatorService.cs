using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public class TankEstimatorService : ITankEstimatorService
{
    private readonly IDataService _dataService;
    private readonly IWeatherService _weatherService;

    public TankEstimatorService(IDataService dataService, IWeatherService weatherService)
    {
        _dataService = dataService;
        _weatherService = weatherService;
    }

    public async Task<TankStatus> GetCurrentStatusAsync()
    {
        var deliveries = await _dataService.GetDeliveriesAsync();
        var tankCapacity = await _dataService.GetTankCapacityAsync();
        var weatherData = await _dataService.GetWeatherHistoryAsync();

        var status = new TankStatus
        {
            TankCapacity = tankCapacity
        };

        if (deliveries.Count == 0)
        {
            status.EstimatedGallons = 0;
            return status;
        }

        // Get the most recent delivery
        var lastDelivery = deliveries.OrderByDescending(d => d.Date).First();
        status.LastDeliveryDate = lastDelivery.Date;
        status.DaysSinceLastDelivery = (int)(DateTime.Today - lastDelivery.Date.Date).TotalDays;

        // Calculate average burn rate
        var burnRate = await GetAverageBurnRateAsync();
        status.EstimatedBurnRate = burnRate;

        // Calculate average K-Factor
        status.AverageKFactor = await GetAverageKFactorAsync();

        // Calculate tank level after last delivery
        decimal tankLevelAfterDelivery;
        if (lastDelivery.FilledToCapacity)
        {
            // Tank was filled to capacity
            tankLevelAfterDelivery = tankCapacity;
        }
        else
        {
            // Partial fill - need to estimate what tank was at before delivery
            // Calculate level at delivery time by walking through delivery history
            var levelBeforeDelivery = await CalculateLevelAtDateAsync(lastDelivery.Date, excludeDeliveryId: lastDelivery.Id);
            tankLevelAfterDelivery = Math.Min(tankCapacity, levelBeforeDelivery + lastDelivery.Gallons);
        }

        // Estimate current gallons by subtracting usage since last delivery
        var estimatedUsage = burnRate * status.DaysSinceLastDelivery;
        status.EstimatedGallons = Math.Max(0, tankLevelAfterDelivery - estimatedUsage);

        // Calculate days remaining
        if (burnRate > 0)
        {
            status.EstimatedDaysRemaining = (int)(status.EstimatedGallons / burnRate);
        }

        return status;
    }

    /// <summary>
    /// Calculates the estimated tank level at a specific date by walking through delivery history.
    /// </summary>
    private async Task<decimal> CalculateLevelAtDateAsync(DateTime targetDate, Guid? excludeDeliveryId = null)
    {
        var deliveries = await _dataService.GetDeliveriesAsync();
        var tankCapacity = await _dataService.GetTankCapacityAsync();
        var burnRate = await GetAverageBurnRateAsync();

        // Filter and sort deliveries before target date
        var relevantDeliveries = deliveries
            .Where(d => d.Date < targetDate && d.Id != excludeDeliveryId)
            .OrderBy(d => d.Date)
            .ToList();

        if (relevantDeliveries.Count == 0)
        {
            // No prior deliveries - assume tank was empty or at some default
            return 0;
        }

        // Start from the most recent "filled to capacity" delivery and work forward
        decimal currentLevel = 0;
        DateTime? lastKnownDate = null;

        foreach (var delivery in relevantDeliveries)
        {
            if (lastKnownDate.HasValue)
            {
                // Subtract usage between last known date and this delivery
                var daysBetween = (decimal)(delivery.Date - lastKnownDate.Value).TotalDays;
                currentLevel = Math.Max(0, currentLevel - (burnRate * daysBetween));
            }

            // Apply this delivery
            if (delivery.FilledToCapacity)
            {
                currentLevel = tankCapacity;
            }
            else
            {
                currentLevel = Math.Min(tankCapacity, currentLevel + delivery.Gallons);
            }

            lastKnownDate = delivery.Date;
        }

        // Now subtract usage from last delivery to target date
        if (lastKnownDate.HasValue)
        {
            var daysToTarget = (decimal)(targetDate - lastKnownDate.Value).TotalDays;
            currentLevel = Math.Max(0, currentLevel - (burnRate * daysToTarget));
        }

        return currentLevel;
    }

    public async Task<DateTime?> PredictRefillDateAsync(decimal thresholdGallons)
    {
        var status = await GetCurrentStatusAsync();

        if (status.EstimatedBurnRate <= 0)
            return null;

        // If already below threshold, return today
        if (status.EstimatedGallons <= thresholdGallons)
            return DateTime.Today;

        // Calculate days until we hit the threshold
        var gallonsUntilThreshold = status.EstimatedGallons - thresholdGallons;
        var daysUntilThreshold = (int)(gallonsUntilThreshold / status.EstimatedBurnRate);

        return DateTime.Today.AddDays(daysUntilThreshold);
    }

    public async Task<decimal> GetAverageBurnRateAsync()
    {
        var deliveries = await _dataService.GetDeliveriesAsync();

        if (deliveries.Count < 2)
            return 0;

        // Sort by date ascending to calculate burn rates between consecutive deliveries
        var sortedDeliveries = deliveries.OrderBy(d => d.Date).ToList();

        // Calculate burn rates for each period between deliveries
        var burnRates = new List<(decimal Rate, DateTime Date)>();
        for (int i = 1; i < sortedDeliveries.Count; i++)
        {
            var prev = sortedDeliveries[i - 1];
            var curr = sortedDeliveries[i];

            var daysBetween = (decimal)(curr.Date - prev.Date).TotalDays;
            if (daysBetween > 0)
            {
                // The gallons delivered at curr is approximately what was used since prev
                var burnRate = curr.Gallons / daysBetween;
                burnRates.Add((burnRate, curr.Date));
            }
        }

        if (burnRates.Count == 0)
            return 0;

        // Use weighted average with more recent deliveries weighted higher
        // Take last 5 deliveries and apply exponential decay weights
        var recentRates = burnRates.OrderByDescending(x => x.Date).Take(5).ToList();
        var weights = new[] { 1.0m, 0.8m, 0.6m, 0.4m, 0.2m };

        var weightedSum = 0m;
        var weightSum = 0m;

        for (int i = 0; i < recentRates.Count; i++)
        {
            var weight = weights[i];
            weightedSum += recentRates[i].Rate * weight;
            weightSum += weight;
        }

        return weightSum > 0 ? weightedSum / weightSum : 0;
    }

    public async Task<decimal?> GetAverageKFactorAsync()
    {
        var deliveries = await _dataService.GetDeliveriesAsync();
        var weatherData = await _dataService.GetWeatherHistoryAsync();

        if (deliveries.Count < 2 || weatherData.Count == 0)
            return null;

        // Sort by date ascending
        var sortedDeliveries = deliveries.OrderBy(d => d.Date).ToList();

        var kFactors = new List<decimal>();
        for (int i = 1; i < sortedDeliveries.Count; i++)
        {
            var prev = sortedDeliveries[i - 1];
            var curr = sortedDeliveries[i];

            var hdd = _weatherService.CalculateHDD(weatherData, prev.Date, curr.Date);

            // Only include K-factors where we have meaningful HDD data
            // (filtering out summer months where HDD is too low)
            if (hdd >= 200)
            {
                var kFactor = _weatherService.CalculateKFactor(curr.Gallons, hdd);
                if (kFactor > 0)
                {
                    kFactors.Add(kFactor);
                }
            }
        }

        if (kFactors.Count == 0)
            return null;

        return kFactors.Average();
    }
}
