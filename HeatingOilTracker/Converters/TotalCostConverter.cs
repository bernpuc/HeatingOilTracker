using System.Globalization;
using System.Windows.Data;

namespace HeatingOilTracker.Converters;

public class TotalCostConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is decimal gallons && values[1] is decimal pricePerGallon)
        {
            var total = gallons * pricePerGallon;
            var formatCulture = GetCulture();
            return total.ToString("C2", formatCulture);
        }

        var defaultCulture = GetCulture();
        return 0m.ToString("C2", defaultCulture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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
