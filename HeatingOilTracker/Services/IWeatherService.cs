using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public interface IWeatherService
{
    /// <summary>
    /// Fetches historical weather data for a date range.
    /// </summary>
    Task<List<DailyWeather>> GetHistoricalWeatherAsync(decimal latitude, decimal longitude, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Calculates total HDD between two dates using stored weather data with default base (65°F).
    /// </summary>
    decimal CalculateHDD(List<DailyWeather> weatherData, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Calculates total HDD between two dates using a custom base temperature.
    /// </summary>
    /// <param name="baseTemperatureF">Base temperature in Fahrenheit (e.g., 65 for US, 64.4 for 18°C)</param>
    decimal CalculateHDD(List<DailyWeather> weatherData, DateTime startDate, DateTime endDate, decimal baseTemperatureF);

    /// <summary>
    /// Calculates K-Factor (HDD per gallon) for a delivery. Industry standard measure of heating efficiency.
    /// </summary>
    decimal CalculateKFactor(decimal gallonsDelivered, decimal hddAccumulated);

    /// <summary>
    /// Searches for locations by city/place name using Open-Meteo geocoding.
    /// Returns multiple matches for user selection.
    /// </summary>
    /// <param name="searchQuery">City or place name to search for</param>
    /// <param name="maxResults">Maximum number of results to return (default: 5)</param>
    Task<List<Location>> SearchLocationsAsync(string searchQuery, int maxResults = 5);
}
