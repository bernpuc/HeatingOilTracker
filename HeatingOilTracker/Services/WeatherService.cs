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
        return weatherData
            .Where(w => w.Date >= startDate.Date && w.Date <= endDate.Date)
            .Sum(w => w.HDD);
    }

    public decimal CalculateKFactor(decimal gallonsDelivered, decimal hddAccumulated)
    {
        if (gallonsDelivered <= 0) return 0;
        return hddAccumulated / gallonsDelivered;
    }

    public async Task<Location?> GeocodePostalCodeAsync(string postalCode, string countryCode = "US")
    {
        // Use Zippopotam.us for postal code lookup (free, no API key)
        // Supports multiple countries: US, GB, CA, DE, FR, NL, BE, AT, CH, etc.
        var url = $"https://api.zippopotam.us/{countryCode.ToLower()}/{postalCode}";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            var places = doc.RootElement.GetProperty("places");
            if (places.GetArrayLength() > 0)
            {
                var place = places[0];
                var lat = decimal.Parse(place.GetProperty("latitude").GetString()!);
                var lon = decimal.Parse(place.GetProperty("longitude").GetString()!);
                var city = place.GetProperty("place name").GetString();

                // State/region field varies by country
                string? region = null;
                if (place.TryGetProperty("state abbreviation", out var stateAbbr))
                    region = stateAbbr.GetString();
                else if (place.TryGetProperty("state", out var state))
                    region = state.GetString();

                var displayName = string.IsNullOrEmpty(region)
                    ? $"{city} {postalCode}"
                    : $"{city}, {region} {postalCode}";

                return new Location
                {
                    Latitude = lat,
                    Longitude = lon,
                    DisplayName = displayName
                };
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            System.Diagnostics.Debug.WriteLine($"Postal code not found: {postalCode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
        }

        return null;
    }
}
