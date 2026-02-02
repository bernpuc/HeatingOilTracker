using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public class ReportService : IReportService
{
    private readonly IDataService _dataService;
    private readonly IWeatherService _weatherService;

    public ReportService(IDataService dataService, IWeatherService weatherService)
    {
        _dataService = dataService;
        _weatherService = weatherService;
    }

    public async Task<YearlySummary> GetYearlySummaryAsync(int year)
    {
        var deliveries = await _dataService.GetDeliveriesAsync();
        var weatherData = await _dataService.GetWeatherHistoryAsync();

        var yearDeliveries = deliveries.Where(d => d.Date.Year == year).ToList();

        var summary = new YearlySummary
        {
            Year = year,
            TotalGallons = yearDeliveries.Sum(d => d.Gallons),
            TotalCost = yearDeliveries.Sum(d => d.Gallons * d.PricePerGallon),
            DeliveryCount = yearDeliveries.Count
        };

        // Calculate HDD for the year if weather data available
        if (weatherData.Count > 0)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            summary.TotalHDD = _weatherService.CalculateHDD(weatherData, startDate, endDate);
        }

        return summary;
    }

    public async Task<List<YearlySummary>> GetAllYearlySummariesAsync()
    {
        var years = await GetAvailableYearsAsync();
        var summaries = new List<YearlySummary>();

        foreach (var year in years.OrderByDescending(y => y))
        {
            var summary = await GetYearlySummaryAsync(year);
            summaries.Add(summary);
        }

        return summaries;
    }

    public async Task<SeasonalBreakdown> GetSeasonalBreakdownAsync(int year)
    {
        var deliveries = await _dataService.GetDeliveriesAsync();
        var weatherData = await _dataService.GetWeatherHistoryAsync();

        var yearDeliveries = deliveries.Where(d => d.Date.Year == year).ToList();

        // Heating season: October - March (months 10, 11, 12, 1, 2, 3)
        // Off-season: April - September (months 4, 5, 6, 7, 8, 9)
        var heatingSeasonMonths = new[] { 10, 11, 12, 1, 2, 3 };

        var heatingDeliveries = yearDeliveries.Where(d => heatingSeasonMonths.Contains(d.Date.Month)).ToList();
        var offSeasonDeliveries = yearDeliveries.Where(d => !heatingSeasonMonths.Contains(d.Date.Month)).ToList();

        var breakdown = new SeasonalBreakdown
        {
            Year = year,
            HeatingSeasonGallons = heatingDeliveries.Sum(d => d.Gallons),
            HeatingSeasonCost = heatingDeliveries.Sum(d => d.Gallons * d.PricePerGallon),
            HeatingSeasonDeliveries = heatingDeliveries.Count,
            OffSeasonGallons = offSeasonDeliveries.Sum(d => d.Gallons),
            OffSeasonCost = offSeasonDeliveries.Sum(d => d.Gallons * d.PricePerGallon),
            OffSeasonDeliveries = offSeasonDeliveries.Count
        };

        // Calculate HDD for each season if weather data available
        if (weatherData.Count > 0)
        {
            // Heating season HDD (Oct-Dec of year + Jan-Mar of year)
            var hddOctDec = _weatherService.CalculateHDD(weatherData,
                new DateTime(year, 10, 1), new DateTime(year, 12, 31));
            var hddJanMar = _weatherService.CalculateHDD(weatherData,
                new DateTime(year, 1, 1), new DateTime(year, 3, 31));
            breakdown.HeatingSeasonHDD = hddOctDec + hddJanMar;

            // Off-season HDD (Apr-Sep)
            breakdown.OffSeasonHDD = _weatherService.CalculateHDD(weatherData,
                new DateTime(year, 4, 1), new DateTime(year, 9, 30));
        }

        return breakdown;
    }

    public async Task<List<int>> GetAvailableYearsAsync()
    {
        var deliveries = await _dataService.GetDeliveriesAsync();

        return deliveries
            .Select(d => d.Date.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();
    }
}
