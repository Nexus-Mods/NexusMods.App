using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public abstract class AFormattedValueComponent<T> : ReactiveR3Object, IItemModelComponent
    where T : notnull
{
    public BindableReactiveProperty<T> Value { get; }
    public BindableReactiveProperty<string> FormattedValue { get; }

    private readonly IDisposable? _activationDisposable;
    protected AFormattedValueComponent(T initialValue, string initialFormattedValue, IObservable<T> valueObservable, bool subscribeWhenCreated = false) : this(initialValue, initialFormattedValue, valueObservable.ToObservable(), subscribeWhenCreated) { }
    protected AFormattedValueComponent(T initialValue, string initialFormattedValue, Observable<T> valueObservable, bool subscribeWhenCreated = false)
    {
        if (!subscribeWhenCreated)
        {
            Value = new BindableReactiveProperty<T>(value: initialValue);
            FormattedValue = new BindableReactiveProperty<string>(value: initialFormattedValue);

            _activationDisposable = this.WhenActivated(valueObservable, static (self, valueObservable, disposables) =>
            {
                valueObservable.ObserveOnUIThreadDispatcher().Subscribe(self, static (value, self) =>
                {
                    self.Value.Value = value;
                    self.FormattedValue.Value = self.FormatValue(value);
                }).AddTo(disposables);
            });
        }
        else
        {
            Value = valueObservable.ToBindableReactiveProperty(initialValue: initialValue);
            FormattedValue = Value
                .Select(this, static (value, self) => self.FormatValue(value))
                .ToBindableReactiveProperty(initialValue: initialFormattedValue);
        }
    }

    protected AFormattedValueComponent(T value, string formattedValue)
    {
        Value = new BindableReactiveProperty<T>(value: value);
        FormattedValue = new BindableReactiveProperty<string>(value: formattedValue);
    }

    protected abstract string FormatValue(T value);

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _activationDisposable?.Dispose();
                Disposable.Dispose(Value, FormattedValue);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
