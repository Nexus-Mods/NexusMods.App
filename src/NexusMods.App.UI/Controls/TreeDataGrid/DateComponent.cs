using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for dates.
/// </summary>
[PublicAPI]
public class DateComponent : ReactiveR3Object, IItemModelComponent<DateComponent>, IComparable<DateComponent>
{
    public BindableReactiveProperty<DateTimeOffset> Value { get; }
    public BindableReactiveProperty<string> FormattedValue { get; }

    private readonly IDisposable _activationDisposable;
    public DateComponent(DateTimeOffset initialValue, IObservable<DateTimeOffset> valueObservable, bool subscribeWhenCreated = false) : this(initialValue, valueObservable.ToObservable(), subscribeWhenCreated) { }
    public DateComponent(DateTimeOffset initialValue, Observable<DateTimeOffset> valueObservable, bool subscribeWhenCreated = false)
    {
        if (!subscribeWhenCreated)
        {
            Value = new BindableReactiveProperty<DateTimeOffset>(value: initialValue);
            FormattedValue = new BindableReactiveProperty<string>(value: initialValue.FormatDate(now: TimeProvider.System.GetLocalNow()));

            _activationDisposable = this.WhenActivated(valueObservable, static (self, valueObservable, disposables) =>
            {
                valueObservable.ObserveOnUIThreadDispatcher().CombineLatest(Tickers.Primary, static (value, now) => (value, now)).Subscribe(self, static (tuple, self) =>
                {
                    var (value, now) = tuple;
                    self.Value.Value = value;
                    self.FormattedValue.Value = value.FormatDate(now: now);
                }).AddTo(disposables);

                self.FormattedValue.Value = self.Value.Value.FormatDate(now: TimeProvider.System.GetLocalNow());
            });
        }
        else
        {
            Value = valueObservable.ToBindableReactiveProperty(initialValue);
            FormattedValue = Value.Select(static value => value.FormatDate(now: TimeProvider.System.GetLocalNow())).ToBindableReactiveProperty(initialValue: initialValue.FormatDate(now: TimeProvider.System.GetLocalNow()));

            _activationDisposable = this.WhenActivated(static (self, disposables) =>
            {
                Tickers.Primary.Subscribe(self, static (now, self) =>
                {
                    self.FormattedValue.Value = self.Value.Value.FormatDate(now: now);
                }).AddTo(disposables);

                self.FormattedValue.Value = self.Value.Value.FormatDate(now: TimeProvider.System.GetLocalNow());
            });
        }
    }

    public DateComponent(DateTimeOffset value)
    {
        Value = new BindableReactiveProperty<DateTimeOffset>(value: value);
        FormattedValue = new BindableReactiveProperty<string>(value: value.FormatDate(now: TimeProvider.System.GetLocalNow()));

        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            Tickers.Primary.Subscribe(self, static (now, self) =>
            {
                self.FormattedValue.Value = self.Value.Value.FormatDate(now: now);
            }).AddTo(disposables);

            self.FormattedValue.Value = self.Value.Value.FormatDate(now: TimeProvider.System.GetLocalNow());
        });
    }

    public int CompareTo(DateComponent? other) => DateTimeOffset.Compare(Value.Value, other?.Value.Value ?? DateTimeOffset.UnixEpoch);

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(_activationDisposable, Value, FormattedValue);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
