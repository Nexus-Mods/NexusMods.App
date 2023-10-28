using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace NexusMods.App.UI.Converters;

public sealed class TextCaseConverter : IValueConverter
{
    public static readonly TextCaseConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string sourceText || parameter is not string targetCase
                                           || !targetType.IsAssignableTo(typeof(string)))
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);

        return targetCase switch
        {
            "upper" => sourceText.ToUpper(CultureInfo.CurrentUICulture),
            "lower" => sourceText.ToLower(CultureInfo.CurrentUICulture),
            "title" => CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(sourceText),
            _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException(), BindingErrorType.Error);
    }
}

public sealed class UpperCaseTextConverter : IValueConverter
{
    public static readonly UpperCaseTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string sourceText || !targetType.IsAssignableTo(typeof(string)))
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);

        return sourceText.ToUpper(CultureInfo.CurrentUICulture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException(), BindingErrorType.Error);
    }
}
