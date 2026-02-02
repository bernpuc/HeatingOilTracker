namespace HeatingOilTracker.Models;

public class OilDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public decimal Gallons { get; set; }
    public decimal PricePerGallon { get; set; }
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the tank was filled to capacity at this delivery.
    /// If true, tank level after delivery = TankCapacity.
    /// If false, tank level after delivery = previous estimate + gallons delivered.
    /// Defaults to true for backwards compatibility.
    /// </summary>
    public bool FilledToCapacity { get; set; } = true;
}
