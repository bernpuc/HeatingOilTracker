using HeatingOilTracker.Models;
using System.Net.Http;
using System.Text.Json;

namespace HeatingOilTracker.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "HeatingOilTracker/1.0");
    }

    public async Task<List<DailyWeather>> GetHistoricalWeatherAsync(
        decimal latitude, decimal longitude, DateTime startDate, DateTime endDate)
    {
        var results = new List<DailyWeather>();

        // Open-Meteo historical API
        // Docs: https://open-meteo.com/en/docs/historical-weather-api
        var url = $"https://archive-api.open-meteo.com/v1/archive?" +
                  $"latitude={latitude:F4}&longitude={longitude:F4}" +
                  $"&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}" +
                  $"&daily=temperature_2m_max,temperature_2m_min" +
                  $"&temperature_unit=fahrenheit&timezone=auto";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            var daily = doc.RootElement.GetProperty("daily");
            var dates = daily.GetProperty("time").EnumerateArray().ToList();
            var maxTemps = daily.GetProperty("temperature_2m_max").EnumerateArray().ToList();
            var minTemps = daily.GetProperty("temperature_2m_min").EnumerateArray().ToList();

            for (int i = 0; i < dates.Count; i++)
            {
                var date = DateTime.Parse(dates[i].GetString()!);
                var high = maxTemps[i].ValueKind == JsonValueKind.Null ? 65m : (decimal)maxTemps[i].GetDouble();
                var low = minTemps[i].ValueKind == JsonValueKind.Null ? 65m : (decimal)minTemps[i].GetDouble();

                results.Add(new DailyWeather
                {
                    Date = date,
                    HighTempF = high,
                    LowTempF = low
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather API error: {ex.Message}");
        }

        return results;
    }

    public decimal CalculateHDD(List<DailyWeather> weatherData, DateTime startDate, DateTime endDate)
    {
        return CalculateHDD(weatherData, startDate, endDate, DailyWeather.DefaultHddBaseF);
    }

    public decimal CalculateHDD(List<DailyWeather> weatherData, DateTime startDate, DateTime endDate, decimal baseTemperatureF)
    {
        return weatherData
            .Where(w => w.Date >= startDate.Date && w.Date <= endDate.Date)
            .Sum(w => w.CalculateHDD(baseTemperatureF));
    }

    public decimal CalculateKFactor(decimal gallonsDelivered, decimal hddAccumulated)
    {
        if (gallonsDelivered <= 0) return 0;
        return hddAccumulated / gallonsDelivered;
    }

    public async Task<List<Location>> SearchLocationsAsync(string searchQuery, int maxResults = 5)
    {
        var results = new List<Location>();

        // Use Open-Meteo's free geocoding API (worldwide coverage)
        // Docs: https://open-meteo.com/en/docs/geocoding-api
        var encodedQuery = Uri.EscapeDataString(searchQuery);
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={encodedQuery}&count={maxResults}&language=en&format=json";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("results", out var resultsArray))
            {
                foreach (var result in resultsArray.EnumerateArray())
                {
                    var name = result.GetProperty("name").GetString() ?? "";
                    var lat = (decimal)result.GetProperty("latitude").GetDouble();
                    var lon = (decimal)result.GetProperty("longitude").GetDouble();

                    // Build display name from available fields
                    var parts = new List<string> { name };

                    if (result.TryGetProperty("admin1", out var admin1) && admin1.GetString() is string region)
                        parts.Add(region);

                    if (result.TryGetProperty("country", out var country) && country.GetString() is string countryName)
                        parts.Add(countryName);

                    results.Add(new Location
                    {
                        Latitude = lat,
                        Longitude = lon,
                        DisplayName = string.Join(", ", parts)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
        }

        return results;
    }
}
