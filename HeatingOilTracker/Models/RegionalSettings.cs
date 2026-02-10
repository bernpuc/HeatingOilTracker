namespace HeatingOilTracker.Models;

/// <summary>
/// Regional settings for internationalization support.
/// </summary>
public class RegionalSettings
{
    /// <summary>
    /// Culture code for currency formatting (e.g., "en-US", "en-GB", "de-DE").
    /// </summary>
    public string CultureCode { get; set; } = "en-US";
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
