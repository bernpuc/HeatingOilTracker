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

    /// <summary>
    /// CO2 emission factor for the selected fuel type (lbs per gallon).
    /// </summary>
    public decimal CO2LbsPerGallon { get; set; } = YearlySummary.DefaultCO2LbsPerGallon;

    // Carbon footprint metrics
    public decimal HeatingSeasonCO2Lbs => HeatingSeasonGallons * CO2LbsPerGallon;
    public decimal OffSeasonCO2Lbs => OffSeasonGallons * CO2LbsPerGallon;
    public decimal TotalCO2Lbs => TotalGallons * CO2LbsPerGallon;
    public decimal HeatingSeasonCO2Percent => TotalCO2Lbs > 0 ? (HeatingSeasonCO2Lbs / TotalCO2Lbs) * 100 : 0;
    public decimal OffSeasonCO2Percent => TotalCO2Lbs > 0 ? (OffSeasonCO2Lbs / TotalCO2Lbs) * 100 : 0;
}
