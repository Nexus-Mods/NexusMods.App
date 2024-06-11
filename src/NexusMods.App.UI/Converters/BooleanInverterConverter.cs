using System.Globalization;
using Avalonia.Data.Converters;

namespace NexusMods.App.UI.Converters;

public class BooleanInverterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }
}
