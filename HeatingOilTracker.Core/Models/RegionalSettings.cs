namespace HeatingOilTracker.Core.Models;

/// <summary>
/// Regional settings for internationalization support.
/// </summary>
public class RegionalSettings
{
    /// <summary>
    /// Culture code for currency formatting (e.g., "en-US", "en-GB", "de-DE").
    /// </summary>
    public string CultureCode { get; set; } = "en-US";

    /// <summary>
    /// Temperature unit preference: "F" for Fahrenheit, "C" for Celsius.
    /// </summary>
    public string TemperatureUnit { get; set; } = "F";

    /// <summary>
    /// Fuel type code for CO2 calculations.
    /// </summary>
    public string FuelTypeCode { get; set; } = "OIL";

    /// <summary>
    /// First month of the heating season (1–12). Default 10 (October) for Northern Hemisphere.
    /// </summary>
    public int HeatingSeasonStartMonth { get; set; } = 10;

    /// <summary>
    /// Last month of the heating season (1–12). Default 3 (March) for Northern Hemisphere.
    /// </summary>
    public int HeatingSeasonEndMonth { get; set; } = 3;

    /// <summary>
    /// Returns true if the given month falls within the configured heating season.
    /// Handles both year-wrapping (e.g. Oct–Mar) and non-wrapping (e.g. Apr–Sep) ranges.
    /// </summary>
    public bool IsHeatingSeason(int month) =>
        HeatingSeasonStartMonth > HeatingSeasonEndMonth
            ? month >= HeatingSeasonStartMonth || month <= HeatingSeasonEndMonth
            : month >= HeatingSeasonStartMonth && month <= HeatingSeasonEndMonth;
}

/// <summary>
/// Supported cultures for currency formatting.
/// </summary>
public static class SupportedCultures
{
    public static readonly List<CultureOption> All = new()
    {
        new("en-US", "US Dollar ($)"),
        new("en-GB", "British Pound (£)"),
        new("en-CA", "Canadian Dollar ($)"),
        new("de-DE", "Euro - Germany (€)"),
        new("fr-FR", "Euro - France (€)"),
        new("nl-NL", "Euro - Netherlands (€)"),
        new("de-AT", "Euro - Austria (€)"),
        new("de-CH", "Swiss Franc (CHF)"),
        new("en-AU", "Australian Dollar ($)"),
        new("en-IE", "Euro - Ireland (€)"),
    };

    public static CultureOption GetByCode(string code) =>
        All.FirstOrDefault(c => c.Code == code) ?? All[0];
}

public record CultureOption(string Code, string DisplayName);

/// <summary>
/// Temperature unit options.
/// </summary>
public static class TemperatureUnits
{
    public static readonly List<TemperatureUnitOption> All = new()
    {
        new("F", "Fahrenheit (°F)", 65m),   // US standard HDD base
        new("C", "Celsius (°C)", 18m),       // International HDD base (18°C ≈ 64.4°F)
    };

    public static TemperatureUnitOption GetByCode(string code) =>
        All.FirstOrDefault(u => u.Code == code) ?? All[0];
}

/// <summary>
/// Temperature unit with HDD base temperature.
/// </summary>
/// <param name="Code">Unit code (F or C)</param>
/// <param name="DisplayName">Display name for UI</param>
/// <param name="HddBase">Base temperature for HDD calculation in this unit</param>
public record TemperatureUnitOption(string Code, string DisplayName, decimal HddBase);

/// <summary>
/// Supported fuel types with CO2 emission factors.
/// </summary>
public static class FuelTypes
{
    public static readonly List<FuelTypeOption> All = new()
    {
        new("OIL", "Heating Oil (#2)", 22.38m, "U.S. EIA"),
        new("KEROSENE", "Kerosene (#1)", 21.54m, "U.S. EIA"),
        new("PROPANE", "Propane", 12.43m, "U.S. EIA"),
    };

    public static FuelTypeOption GetByCode(string code) =>
        All.FirstOrDefault(f => f.Code == code) ?? All[0];
}

/// <summary>
/// Fuel type with CO2 emission factor.
/// </summary>
/// <param name="Code">Fuel type code</param>
/// <param name="DisplayName">Display name for UI</param>
/// <param name="CO2LbsPerGallon">CO2 emissions in lbs per gallon</param>
/// <param name="Source">Source of emission factor</param>
public record FuelTypeOption(string Code, string DisplayName, decimal CO2LbsPerGallon, string Source);

/// <summary>
/// Month option for heating season picker.
/// </summary>
public static class MonthOptions
{
    public static readonly List<MonthOption> All = new()
    {
        new(1, "January"), new(2, "February"), new(3, "March"),
        new(4, "April"),   new(5, "May"),       new(6, "June"),
        new(7, "July"),    new(8, "August"),    new(9, "September"),
        new(10, "October"), new(11, "November"), new(12, "December"),
    };

    public static MonthOption GetByNumber(int month) =>
        All.FirstOrDefault(m => m.Number == month) ?? All[9]; // October fallback
}

public record MonthOption(int Number, string DisplayName);
