using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public interface ITankEstimatorService
{
    /// <summary>
    /// Gets the current estimated tank status based on delivery history and burn rate.
    /// </summary>
    Task<TankStatus> GetCurrentStatusAsync();

    /// <summary>
    /// Predicts when the tank will need a refill based on a threshold.
    /// </summary>
    Task<DateTime?> PredictRefillDateAsync(decimal thresholdGallons);

    /// <summary>
    /// Gets the average daily burn rate based on recent deliveries.
    /// </summary>
    Task<decimal> GetAverageBurnRateAsync();

    /// <summary>
    /// Gets the average K-Factor (gallons per HDD) based on delivery history.
    /// </summary>
    Task<decimal?> GetAverageKFactorAsync();
}
