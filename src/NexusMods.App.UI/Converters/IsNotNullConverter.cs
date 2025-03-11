using System.Globalization;
using Avalonia.Data.Converters;

namespace NexusMods.App.UI.Converters;

public class IsNotNullConverter : IValueConverter
{
    public static readonly IsNotNullConverter Instance = new IsNotNullConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
