using System.Globalization;
using System.Windows.Data;

namespace HeatingOilTracker.Converters;

public class PercentToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal percent && parameter is string maxHeightStr)
        {
            if (double.TryParse(maxHeightStr, out var maxHeight))
            {
                // Clamp percent between 0 and 100
                var clampedPercent = Math.Max(0, Math.Min(100, (double)percent));
                return (clampedPercent / 100.0) * maxHeight;
            }
        }

        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
