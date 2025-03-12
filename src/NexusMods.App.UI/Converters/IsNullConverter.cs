using System.Globalization;
using Avalonia.Data.Converters;

namespace NexusMods.App.UI.Converters;

public class IsNullConverter : IValueConverter
{
    public static readonly IsNullConverter Instance = new IsNullConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
