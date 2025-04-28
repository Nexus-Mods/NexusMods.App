using System.Globalization;
using Avalonia.Data.Converters;

namespace NexusMods.Themes.NexusFluentDark.Converters;

/// <summary>
/// Converts the indent level of a TreeDataGrid item to a boolean value.
/// True if the indent level is greater than 0, false otherwise.
/// </summary>
public class TreeDataGridIndentToBoolConverter : IValueConverter
{
    public static TreeDataGridIndentToBoolConverter Instance { get; } = new TreeDataGridIndentToBoolConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int indent)
        {
            return indent > 0;
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
