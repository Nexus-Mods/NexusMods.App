using System.Globalization;
using Humanizer;
using Humanizer.Bytes;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Extensions;

public static class FormatExtensions
{
    public static string FormatDate(this DateTimeOffset date, DateTimeOffset now)
    {
        if (date == DateTimeOffset.MinValue || date == DateTimeOffset.MaxValue || date == DateTimeOffset.UnixEpoch) return "-";
        return date.Humanize(dateToCompareAgainst: now, culture: CultureInfo.CurrentUICulture);
    }

    public static BindableReactiveProperty<string> ToFormattedProperty(this Observable<DateTimeOffset> source)
    {
        return source
            .Select(static date => date.FormatDate(now: TimeProvider.System.GetLocalNow()))
            .ToBindableReactiveProperty(initialValue: "");
    }

    public static BindableReactiveProperty<string> ToFormattedProperty(this Observable<Size> source)
    {
        return source
            .Select(static size => ByteSize.FromBytes(size.Value).Humanize())
            .ToBindableReactiveProperty(initialValue: "");
    }
}
