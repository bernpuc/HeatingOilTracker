namespace HeatingOilTracker.Models;

public class OilDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public decimal Gallons { get; set; }
    public decimal PricePerGallon { get; set; }
    public string Notes { get; set; } = string.Empty;
}
