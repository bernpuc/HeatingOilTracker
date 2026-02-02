namespace HeatingOilTracker.Models;

/// <summary>
/// Annual cost and usage summary for oil deliveries.
/// </summary>
public class YearlySummary
{
    public int Year { get; set; }
    public decimal TotalGallons { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvgPricePerGallon => TotalGallons > 0 ? TotalCost / TotalGallons : 0;
    public int DeliveryCount { get; set; }
    public decimal? TotalHDD { get; set; }
    public decimal? CostPerHDD => TotalHDD > 0 ? TotalCost / TotalHDD : null;
    public decimal? AvgKFactor => TotalHDD > 0 ? TotalGallons / TotalHDD : null;
}
