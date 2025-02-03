using R3;

namespace NexusMods.App.UI;

public static class Tickers
{
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(30);
    public static Observable<DateTimeOffset> Primary { get; }

    static Tickers()
    {
        var observable = Observable
            .Interval(period: Period, timeProvider: ObservableSystem.DefaultTimeProvider)
            .ObserveOnUIThreadDispatcher()
            .Prepend(Unit.Default)
            .Select(_ => TimeProvider.System.GetLocalNow())
            .Multicast(new DateSubject(TimeProvider.System));

        _ = observable.Connect();
        Primary = observable;
    }
}

file class DateSubject : Subject<DateTimeOffset>
{
    private readonly TimeProvider _timeProvider;

    public DateSubject(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    protected override IDisposable SubscribeCore(Observer<DateTimeOffset> observer)
    {
        var disposable = base.SubscribeCore(observer);
        observer.OnNext(_timeProvider.GetLocalNow());
        return disposable;
    }
}
