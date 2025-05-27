using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Humanizer;
using Humanizer.Bytes;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Converters;

/// <summary>
/// Convert DateTimeOffset to a human-readable string representation.
/// </summary>
public class DateTimeOffsetToStringConverter : IValueConverter
{
    public static readonly DateTimeOffsetToStringConverter Instance = new ();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset && targetType.IsAssignableTo(typeof(string)))
        {
            return dateTimeOffset.Humanize();
        }
        
        // converter used for the wrong type
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
