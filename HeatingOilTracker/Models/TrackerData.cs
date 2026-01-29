namespace HeatingOilTracker.Models;

public class TrackerData
{
    public decimal TankCapacityGallons { get; set; } = 275m;
    public List<OilDelivery> Deliveries { get; set; } = new();
}
