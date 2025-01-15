using R3;

namespace NexusMods.App.UI;

public static class Tickers
{
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(30);
    public static Observable<DateTimeOffset> Primary { get; }

    static Tickers()
    {
        Primary = Observable
            .Interval(period: Period, timeProvider: ObservableSystem.DefaultTimeProvider)
            .ObserveOnUIThreadDispatcher()
            .Select(_ => TimeProvider.System.GetLocalNow())
            .Publish(initialValue: TimeProvider.System.GetLocalNow());
    }
}
