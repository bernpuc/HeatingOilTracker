namespace HeatingOilTracker.Models;

public class DailyWeather
{
    /// <summary>
    /// Default HDD base temperature in Fahrenheit (US standard).
    /// </summary>
    public const decimal DefaultHddBaseF = 65m;

    /// <summary>
    /// HDD base temperature in Fahrenheit for Celsius users (18°C converted).
    /// </summary>
    public const decimal CelsiusHddBaseF = 64.4m;

    public DateTime Date { get; set; }
    public decimal HighTempF { get; set; }
    public decimal LowTempF { get; set; }

    public decimal AvgTempF => (HighTempF + LowTempF) / 2;

    /// <summary>
    /// Average temperature in Celsius.
    /// </summary>
    public decimal AvgTempC => (AvgTempF - 32m) * 5m / 9m;

    /// <summary>
    /// Heating Degree Days using default base (65°F).
    /// HDD = max(0, 65 - AvgTemp)
    /// </summary>
    public decimal HDD => Math.Max(0, DefaultHddBaseF - AvgTempF);

    /// <summary>
    /// Calculate HDD with a custom base temperature (in Fahrenheit).
    /// </summary>
    public decimal CalculateHDD(decimal baseTemperatureF) => Math.Max(0, baseTemperatureF - AvgTempF);

    /// <summary>
    /// Get HDD base temperature for a given temperature unit setting.
    /// </summary>
    public static decimal GetHddBase(string temperatureUnit) =>
        temperatureUnit == "C" ? CelsiusHddBaseF : DefaultHddBaseF;
}
