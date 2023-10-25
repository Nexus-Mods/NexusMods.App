using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace NexusMods.App.UI.Icons;

public sealed class IconTypeConverter : IValueConverter
{
    public static readonly IconTypeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IconType iconType)
            return new BindingNotification(new NotSupportedException(), BindingErrorType.Error);

        return iconType.ToMaterialUiName();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException(), BindingErrorType.Error);
    }
}
