using System.Globalization;
using Avalonia.Data.Converters;

namespace NexusMods.App.UI.Converters;


/// <summary>
/// Returns true if the parameter flag is contained in the flag list.
/// The Flags property may need manual raising of property change notifications when the collection is modified to make the binding reactive.
/// </summary>
public class CompositeStyleFlagConverter : IValueConverter
{
    
    public static CompositeStyleFlagConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IReadOnlyList<string> flags || parameter is not string flagName || targetType != typeof(bool))
        {
            throw new ArgumentException();
        }

        return flags.Contains(flagName);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
