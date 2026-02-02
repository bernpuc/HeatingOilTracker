namespace HeatingOilTracker.Models;

public class TrackerData
{
    public decimal TankCapacityGallons { get; set; } = 275m;
    public List<OilDelivery> Deliveries { get; set; } = new();
    public Location Location { get; set; } = new();
    public List<DailyWeather> WeatherHistory { get; set; } = new();
    public ReminderSettings ReminderSettings { get; set; } = new();
}
