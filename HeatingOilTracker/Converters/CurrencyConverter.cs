using System.Globalization;
using System.Windows.Data;

namespace HeatingOilTracker.Converters;

/// <summary>
/// Converts decimal values to culture-aware currency strings.
/// Uses the culture code stored in RegionalSettings.
/// </summary>
public class CurrencyConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the current culture code for formatting.
    /// This should be set from the application's regional settings.
    /// </summary>
    public static string CurrentCultureCode { get; set; } = "en-US";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            var formatCulture = GetCulture();
            return decimalValue.ToString("C2", formatCulture);
        }
        if (value is double doubleValue)
        {
            var formatCulture = GetCulture();
            return doubleValue.ToString("C2", formatCulture);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static CultureInfo GetCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo(CurrentCultureCode);
        }
        catch
        {
            return CultureInfo.GetCultureInfo("en-US");
        }
    }
}

/// <summary>
/// Converts price per gallon to culture-aware currency string with 3 decimal places.
/// </summary>
public class PricePerGallonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            var formatCulture = GetCulture();
            // Use currency symbol but with 3 decimal places for price per gallon
            var symbol = formatCulture.NumberFormat.CurrencySymbol;
            return $"{symbol}{decimalValue:F3}";
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static CultureInfo GetCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo(CurrencyConverter.CurrentCultureCode);
        }
        catch
        {
            return CultureInfo.GetCultureInfo("en-US");
        }
    }
}
