namespace HeatingOilTracker.Models;

/// <summary>
/// Annual cost and usage summary for oil deliveries.
/// </summary>
public class YearlySummary
{
    /// <summary>
    /// CO2 emission factor for heating oil: 22.38 lbs CO2 per gallon burned.
    /// Source: U.S. EIA (Energy Information Administration)
    /// </summary>
    public const decimal CO2LbsPerGallon = 22.38m;

    /// <summary>
    /// Carbon offset price range per metric ton (USD).
    /// Low: Basic offsets (reforestation, renewable energy credits)
    /// High: Premium offsets (verified projects, direct air capture)
    /// </summary>
    public const decimal OffsetPriceLowPerTon = 15m;
    public const decimal OffsetPriceHighPerTon = 50m;

    public int Year { get; set; }
    public decimal TotalGallons { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvgPricePerGallon => TotalGallons > 0 ? TotalCost / TotalGallons : 0;
    public int DeliveryCount { get; set; }
    public decimal? TotalHDD { get; set; }
    public decimal? CostPerHDD => TotalHDD > 0 ? TotalCost / TotalHDD : null;
    public decimal? AvgKFactor => TotalHDD > 0 ? TotalGallons / TotalHDD : null;

    // Carbon footprint metrics
    public decimal TotalCO2Lbs => TotalGallons * CO2LbsPerGallon;
    public decimal TotalCO2Kg => TotalCO2Lbs * 0.453592m;
    public decimal TotalCO2MetricTons => TotalCO2Kg / 1000m;
    public decimal? CO2LbsPerHDD => TotalHDD > 0 ? TotalCO2Lbs / TotalHDD : null;

    // Carbon offset cost estimates
    public decimal OffsetCostLow => TotalCO2MetricTons * OffsetPriceLowPerTon;
    public decimal OffsetCostHigh => TotalCO2MetricTons * OffsetPriceHighPerTon;
}
