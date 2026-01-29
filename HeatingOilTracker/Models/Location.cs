namespace HeatingOilTracker.Models;

public class Location
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public bool IsSet => Latitude != 0 || Longitude != 0;
}
