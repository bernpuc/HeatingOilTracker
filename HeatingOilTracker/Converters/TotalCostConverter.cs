using System.Globalization;
using System.Windows.Data;

namespace HeatingOilTracker.Converters;

public class TotalCostConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is decimal gallons && values[1] is decimal pricePerGallon)
        {
            return (gallons * pricePerGallon).ToString("C2");
        }

        return "$0.00";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
