using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using System.Net.Http;
using System.Text.Json;

namespace HeatingOilTracker.Core.Services;

/// <summary>
/// Fetches No. 2 heating oil retail prices from the EIA API v2.
/// API docs: https://www.eia.gov/opendata/documentation.php
/// Series: petroleum/pri/wfr — Weekly Retail Gasoline and Diesel Prices
/// Product: EPD2F — No. 2 Heating Oil, Retail
/// </summary>
public class EiaService : IEiaService
{
    private const string BaseUrl = "https://api.eia.gov/v2/petroleum/pri/wfr/data/";
    private const string HeatingOilProduct = "EPD2F";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public EiaService(string apiKey) : this(apiKey, new HttpClient()) { }

    public EiaService(string apiKey, HttpClient httpClient)
    {
        _apiKey = apiKey;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "HeatingOilTracker/1.0");
    }

    public async Task<List<EiaHeatingOilPrice>> GetWeeklyPricesAsync(string regionCode, int weeks = 52)
    {
        var url = BuildUrl(regionCode, weeks);
        return await FetchPricesAsync(url);
    }

    public async Task<EiaHeatingOilPrice?> GetLatestPriceAsync(string regionCode)
    {
        var url = BuildUrl(regionCode, length: 1);
        var results = await FetchPricesAsync(url);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<List<EiaHeatingOilPrice>> GetLatestPricesAllRegionsAsync()
    {
        // Fetch one data point per region concurrently
        var tasks = EiaRegion.All.Select(r => GetLatestPriceAsync(r.Code));
        var results = await Task.WhenAll(tasks);
        return results.OfType<EiaHeatingOilPrice>().ToList();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private string BuildUrl(string regionCode, int length = 52) =>
        $"{BaseUrl}?api_key={_apiKey}" +
        $"&frequency=weekly" +
        $"&data[0]=value" +
        $"&facets[product][]={HeatingOilProduct}" +
        $"&facets[duoarea][]={regionCode}" +
        $"&sort[0][column]=period" +
        $"&sort[0][direction]=desc" +
        $"&length={length}";

    private async Task<List<EiaHeatingOilPrice>> FetchPricesAsync(string url)
    {
        var json = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("response", out var response) ||
            !response.TryGetProperty("data", out var dataArray))
            return [];

        var results = new List<EiaHeatingOilPrice>();

        foreach (var item in dataArray.EnumerateArray())
        {
            var periodStr  = item.GetProperty("period").GetString();
            var regionCode = item.GetProperty("duoarea").GetString() ?? string.Empty;
            var regionName = item.GetProperty("area-name").GetString() ?? string.Empty;
            var valueStr   = item.GetProperty("value").GetString();

            if (!DateOnly.TryParse(periodStr, out var period))
                continue;

            // EIA returns value as string; null/"" means data not available for that week
            if (!decimal.TryParse(valueStr, out var price))
                continue;

            results.Add(new EiaHeatingOilPrice(period, regionCode, regionName, price));
        }

        // The WFR series has a 'process' facet we don't filter on, so multiple rows can
        // come back per period (e.g., different sale-type breakdowns). Deduplicate by
        // taking the average price for each (period, duoarea) pair.
        return results
            .GroupBy(r => (r.Period, r.RegionCode))
            .Select(g => new EiaHeatingOilPrice(
                g.Key.Period,
                g.Key.RegionCode,
                g.First().RegionName,
                Math.Round(g.Average(r => r.PricePerGallon), 3)))
            .OrderByDescending(r => r.Period)
            .ToList();
    }
}
