using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public class DateComponent : ReactiveR3Object, IItemModelComponent
{
    public BindableReactiveProperty<DateTimeOffset> Value { get; }
    public BindableReactiveProperty<string> FormattedValue { get; }

    private readonly IDisposable _activationDisposable;
    public DateComponent(IObservable<DateTimeOffset> valueObservable, bool subscribeWhenCreated = false, Optional<DateTimeOffset> initialValue = default) : this(valueObservable.ToObservable(), subscribeWhenCreated, initialValue) { }
    public DateComponent(Observable<DateTimeOffset> valueObservable, bool subscribeWhenCreated = false, Optional<DateTimeOffset> initialValue = default)
    {
        if (!subscribeWhenCreated)
        {
            Value = new BindableReactiveProperty<DateTimeOffset>(value: initialValue.ValueOr(alternativeValue: DateTimeOffset.MinValue));
            FormattedValue = new BindableReactiveProperty<string>(value: initialValue.Convert(date => date.FormatDate(now: TimeProvider.System.GetLocalNow())).ValueOr(alternativeValue: string.Empty));

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
            Value = valueObservable.ToBindableReactiveProperty(initialValue: initialValue.ValueOr(alternativeValue: DateTimeOffset.MinValue));
            FormattedValue = Value.Select(static value => value.FormatDate(now: TimeProvider.System.GetLocalNow())).ToBindableReactiveProperty(initialValue: initialValue.Convert(date => date.FormatDate(now: TimeProvider.System.GetLocalNow())).ValueOr(alternativeValue: string.Empty));

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
