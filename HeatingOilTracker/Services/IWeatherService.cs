using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public interface IWeatherService
{
    /// <summary>
    /// Fetches historical weather data for a date range.
    /// </summary>
    Task<List<DailyWeather>> GetHistoricalWeatherAsync(decimal latitude, decimal longitude, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Calculates total HDD between two dates using stored weather data.
    /// </summary>
    decimal CalculateHDD(List<DailyWeather> weatherData, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Calculates K-Factor (gallons per HDD) for a delivery.
    /// </summary>
    decimal CalculateKFactor(decimal gallonsDelivered, decimal hddAccumulated);

    /// <summary>
    /// Looks up coordinates for a US ZIP code using Open-Meteo geocoding.
    /// </summary>
    Task<Location?> GeocodeZipCodeAsync(string zipCode);
}
