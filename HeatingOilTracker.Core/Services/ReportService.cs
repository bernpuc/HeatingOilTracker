using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;

namespace HeatingOilTracker.Core.Services;

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
        var regionalSettings = await _dataService.GetRegionalSettingsAsync();

        var yearDeliveries = deliveries.Where(d => d.Date.Year == year).ToList();

        // Get CO2 factor from fuel type setting
        var fuelType = FuelTypes.GetByCode(regionalSettings.FuelTypeCode);
        var hddBaseF = DailyWeather.GetHddBase(regionalSettings.TemperatureUnit);

        var summary = new YearlySummary
        {
            Year = year,
            TotalGallons = yearDeliveries.Sum(d => d.Gallons),
            TotalCost = yearDeliveries.Sum(d => d.Gallons * d.PricePerGallon),
            DeliveryCount = yearDeliveries.Count,
            CO2LbsPerGallon = fuelType.CO2LbsPerGallon
        };

        // Calculate HDD for the year if weather data available
        if (weatherData.Count > 0)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            summary.TotalHDD = _weatherService.CalculateHDD(weatherData, startDate, endDate, hddBaseF);
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
        var regionalSettings = await _dataService.GetRegionalSettingsAsync();

        var yearDeliveries = deliveries.Where(d => d.Date.Year == year).ToList();

        // Get CO2 factor from fuel type setting
        var fuelType = FuelTypes.GetByCode(regionalSettings.FuelTypeCode);
        var hddBaseF = DailyWeather.GetHddBase(regionalSettings.TemperatureUnit);

        var heatingDeliveries = yearDeliveries.Where(d => regionalSettings.IsHeatingSeason(d.Date.Month)).ToList();
        var offSeasonDeliveries = yearDeliveries.Where(d => !regionalSettings.IsHeatingSeason(d.Date.Month)).ToList();

        var breakdown = new SeasonalBreakdown
        {
            Year = year,
            HeatingSeasonGallons = heatingDeliveries.Sum(d => d.Gallons),
            HeatingSeasonCost = heatingDeliveries.Sum(d => d.Gallons * d.PricePerGallon),
            HeatingSeasonDeliveries = heatingDeliveries.Count,
            OffSeasonGallons = offSeasonDeliveries.Sum(d => d.Gallons),
            OffSeasonCost = offSeasonDeliveries.Sum(d => d.Gallons * d.PricePerGallon),
            OffSeasonDeliveries = offSeasonDeliveries.Count,
            CO2LbsPerGallon = fuelType.CO2LbsPerGallon
        };

        // Calculate HDD for each season if weather data available
        if (weatherData.Count > 0)
        {
            int s = regionalSettings.HeatingSeasonStartMonth;
            int e = regionalSettings.HeatingSeasonEndMonth;

            if (s > e)
            {
                // Wraps year boundary (e.g. Oct–Mar, Northern Hemisphere):
                // heating = Jan..endMonth + startMonth..Dec of the same calendar year
                breakdown.HeatingSeasonHDD =
                    _weatherService.CalculateHDD(weatherData,
                        new DateTime(year, 1, 1),
                        new DateTime(year, e, DateTime.DaysInMonth(year, e)), hddBaseF) +
                    _weatherService.CalculateHDD(weatherData,
                        new DateTime(year, s, 1),
                        new DateTime(year, 12, 31), hddBaseF);
                // Off-season = endMonth+1 .. startMonth-1 (single contiguous range)
                breakdown.OffSeasonHDD =
                    _weatherService.CalculateHDD(weatherData,
                        new DateTime(year, e + 1, 1),
                        new DateTime(year, s - 1, DateTime.DaysInMonth(year, s - 1)), hddBaseF);
            }
            else
            {
                // No wrap (e.g. Apr–Sep, Southern Hemisphere):
                // heating = startMonth..endMonth (single range)
                breakdown.HeatingSeasonHDD =
                    _weatherService.CalculateHDD(weatherData,
                        new DateTime(year, s, 1),
                        new DateTime(year, e, DateTime.DaysInMonth(year, e)), hddBaseF);
                // Off-season = Jan..startMonth-1 + endMonth+1..Dec
                var hddBefore = s > 1
                    ? _weatherService.CalculateHDD(weatherData,
                        new DateTime(year, 1, 1),
                        new DateTime(year, s - 1, DateTime.DaysInMonth(year, s - 1)), hddBaseF)
                    : 0m;
                var hddAfter = e < 12
                    ? _weatherService.CalculateHDD(weatherData,
                        new DateTime(year, e + 1, 1),
                        new DateTime(year, 12, 31), hddBaseF)
                    : 0m;
                breakdown.OffSeasonHDD = hddBefore + hddAfter;
            }
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
