namespace HeatingOilTracker.Models;

public class DailyWeather
{
    public DateTime Date { get; set; }
    public decimal HighTempF { get; set; }
    public decimal LowTempF { get; set; }

    public decimal AvgTempF => (HighTempF + LowTempF) / 2;

    /// <summary>
    /// Heating Degree Days - measures heating demand.
    /// HDD = max(0, 65 - AvgTemp)
    /// </summary>
    public decimal HDD => Math.Max(0, 65m - AvgTempF);
}
