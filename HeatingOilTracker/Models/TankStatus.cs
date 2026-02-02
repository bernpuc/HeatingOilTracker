namespace HeatingOilTracker.Models;

/// <summary>
/// Represents the current estimated state of the oil tank.
/// </summary>
public class TankStatus
{
    public decimal EstimatedGallons { get; set; }
    public decimal TankCapacity { get; set; }
    public decimal PercentFull => TankCapacity > 0 ? (EstimatedGallons / TankCapacity) * 100 : 0;
    public DateTime? LastDeliveryDate { get; set; }
    public int DaysSinceLastDelivery { get; set; }
    public decimal EstimatedBurnRate { get; set; }  // gal/day
    public int? EstimatedDaysRemaining { get; set; }
    public decimal? AverageKFactor { get; set; }  // gal/HDD
}
