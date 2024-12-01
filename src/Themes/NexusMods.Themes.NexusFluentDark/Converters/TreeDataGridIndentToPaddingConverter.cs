using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace NexusMods.Themes.NexusFluentDark.Converters;

/// <summary>
/// Converts the indent level of a TreeDataGrid item to a padding values for the first column.
/// </summary>
public class TreeDataGridIndentToPaddingConverter : IValueConverter
{
    private const int LeftPadding = 12;
    private const int OffsetWidth = 20;
    private const int RightPadding = 8;

    public static TreeDataGridIndentToPaddingConverter Instance { get; } = new TreeDataGridIndentToPaddingConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int indent) return new Thickness();
        
        // For indent 1 and greater, view has an additional element (indent line) of OffsetWidth width,
        // which we don't want to include in the padding, so we subtract 1 to get the correct offset.
        var leftPadding = LeftPadding + (indent < 1 ? 0 : OffsetWidth * (indent - 1));
        
        return new Thickness(leftPadding, 0, RightPadding, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
