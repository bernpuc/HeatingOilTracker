using HeatingOilTracker.Models;

namespace HeatingOilTracker.Services;

public interface IReportService
{
    /// <summary>
    /// Gets a summary of oil usage and costs for a specific year.
    /// </summary>
    Task<YearlySummary> GetYearlySummaryAsync(int year);

    /// <summary>
    /// Gets summaries for all years with delivery data.
    /// </summary>
    Task<List<YearlySummary>> GetAllYearlySummariesAsync();

    /// <summary>
    /// Gets a seasonal breakdown (heating vs off-season) for a specific year.
    /// </summary>
    Task<SeasonalBreakdown> GetSeasonalBreakdownAsync(int year);

    /// <summary>
    /// Gets a list of all years that have delivery data.
    /// </summary>
    Task<List<int>> GetAvailableYearsAsync();
}
