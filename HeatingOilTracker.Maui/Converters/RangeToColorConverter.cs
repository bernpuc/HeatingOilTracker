using System.Globalization;

namespace HeatingOilTracker.Maui.Converters;

public class RangeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selected = value as string;
        var button = parameter as string;
        return selected == button ? Color.FromArgb("#2563eb") : Color.FromArgb("#94a3b8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}