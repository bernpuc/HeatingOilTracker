namespace HeatingOilTracker.Models;

/// <summary>
/// Breakdown of oil usage between heating season (Oct-Mar) and off-season (Apr-Sep).
/// </summary>
public class SeasonalBreakdown
{
    public int Year { get; set; }

    // Heating Season (October - March)
    public decimal HeatingSeasonGallons { get; set; }
    public decimal HeatingSeasonCost { get; set; }
    public int HeatingSeasonDeliveries { get; set; }
    public decimal? HeatingSeasonHDD { get; set; }

    // Off-Season (April - September)
    public decimal OffSeasonGallons { get; set; }
    public decimal OffSeasonCost { get; set; }
    public int OffSeasonDeliveries { get; set; }
    public decimal? OffSeasonHDD { get; set; }

    // Computed percentages
    public decimal HeatingSeasonCostPercent => TotalCost > 0 ? (HeatingSeasonCost / TotalCost) * 100 : 0;
    public decimal OffSeasonCostPercent => TotalCost > 0 ? (OffSeasonCost / TotalCost) * 100 : 0;

    public decimal TotalCost => HeatingSeasonCost + OffSeasonCost;
    public decimal TotalGallons => HeatingSeasonGallons + OffSeasonGallons;
}
