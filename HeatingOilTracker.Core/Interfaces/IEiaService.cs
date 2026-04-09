using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Core.Interfaces;

/// <summary>
/// Fetches heating oil price data from the U.S. Energy Information Administration (EIA) API v2.
/// Requires a free API key from https://www.eia.gov/opendata/register.php
/// </summary>
public interface IEiaService
{
    /// <summary>
    /// Returns weekly No. 2 heating oil retail prices for the given region.
    /// </summary>
    /// <param name="regionCode">EIA duoarea code. Use <see cref="EiaRegion"/> constants.</param>
    /// <param name="weeks">Number of recent weeks to fetch (max 5000).</param>
    Task<List<EiaHeatingOilPrice>> GetWeeklyPricesAsync(string regionCode, int weeks = 52);

    /// <summary>
    /// Returns the most recent weekly price for the given region.
    /// Returns null if no data is available.
    /// </summary>
    Task<EiaHeatingOilPrice?> GetLatestPriceAsync(string regionCode);

    /// <summary>
    /// Returns the most recent weekly price for all standard regions.
    /// Useful for building a national price map.
    /// </summary>
    Task<List<EiaHeatingOilPrice>> GetLatestPricesAllRegionsAsync();
}
