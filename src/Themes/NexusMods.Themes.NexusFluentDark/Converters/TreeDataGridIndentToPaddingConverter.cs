using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace NexusMods.Themes.NexusFluentDark.Converters;

public class TreeDataGridIndentToPaddingConverter : IValueConverter
{
    public static TreeDataGridIndentToPaddingConverter Instance { get; } = new TreeDataGridIndentToPaddingConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int indent)
        {
            return new Thickness(12 + 24 * indent, 0, 12, 0);
        }

        return new Thickness();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
