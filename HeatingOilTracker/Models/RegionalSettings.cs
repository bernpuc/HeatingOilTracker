namespace HeatingOilTracker.Models;

/// <summary>
/// Regional settings for internationalization support.
/// </summary>
public class RegionalSettings
{
    /// <summary>
    /// ISO 3166-1 alpha-2 country code for postal code lookup.
    /// Supported: US, GB, CA, DE, FR, NL, BE, AT, CH
    /// </summary>
    public string CountryCode { get; set; } = "US";

    /// <summary>
    /// Culture code for currency formatting (e.g., "en-US", "en-GB", "de-DE").
    /// </summary>
    public string CultureCode { get; set; } = "en-US";
}

/// <summary>
/// Supported countries for postal code lookup via Zippopotam.us API.
/// </summary>
public static class SupportedCountries
{
    public static readonly List<CountryInfo> All = new()
    {
        new("US", "United States", "en-US", "ZIP Code"),
        new("CA", "Canada", "en-CA", "Postal Code"),
        new("GB", "United Kingdom", "en-GB", "Postcode"),
        new("DE", "Germany", "de-DE", "Postleitzahl"),
        new("FR", "France", "fr-FR", "Code Postal"),
        new("NL", "Netherlands", "nl-NL", "Postcode"),
        new("BE", "Belgium", "fr-BE", "Postcode"),
        new("AT", "Austria", "de-AT", "Postleitzahl"),
        new("CH", "Switzerland", "de-CH", "Postleitzahl"),
    };

    public static CountryInfo GetByCode(string code) =>
        All.FirstOrDefault(c => c.Code == code) ?? All[0];
}

public record CountryInfo(string Code, string Name, string DefaultCulture, string PostalCodeLabel);
