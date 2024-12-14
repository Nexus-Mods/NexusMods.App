using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public interface IItemModelComponent;

[PublicAPI]
public abstract class AValueComponent<T> : ReactiveR3Object, IItemModelComponent
    where T : notnull
{
    public BindableReactiveProperty<T> Value { get; }

    private readonly IDisposable? _activationDisposable;
    protected AValueComponent(T defaultValue, IObservable<T> valueObservable, bool subscribeWhenCreated = false, Optional<T> initialValue = default) : this(defaultValue, valueObservable.ToObservable(), subscribeWhenCreated, initialValue) { }
    protected AValueComponent(T defaultValue, Observable<T> valueObservable, bool subscribeWhenCreated = false, Optional<T> initialValue = default)
    {
        if (!subscribeWhenCreated)
        {
            Value = new BindableReactiveProperty<T>(value: initialValue.ValueOr(defaultValue));

            _activationDisposable = this.WhenActivated(valueObservable, static (self, valueObservable, disposables) =>
            {
                valueObservable.ObserveOnUIThreadDispatcher().Subscribe(self, static (value, self) =>
                {
                    self.Value.Value = value;
                }).AddTo(disposables);
            });
        }
        else
        {
            Value = valueObservable.ToBindableReactiveProperty(initialValue: initialValue.ValueOr(defaultValue));
        }
    }

    protected AValueComponent(T value)
    {
        Value = new BindableReactiveProperty<T>(value: value);
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _activationDisposable?.Dispose();
                Value.Dispose();
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}

[PublicAPI]
public abstract class AFormattedValueComponent<T> : ReactiveR3Object, IItemModelComponent
    where T : notnull
{
    public BindableReactiveProperty<T> Value { get; }
    public BindableReactiveProperty<string> FormattedValue { get; }

    private readonly IDisposable? _activationDisposable;
    protected AFormattedValueComponent(T defaultValue, IObservable<T> valueObservable, bool subscribeWhenCreated = false, Optional<T> initialValue = default, Optional<string> initialFormattedValue = default) : this(defaultValue, valueObservable.ToObservable(), subscribeWhenCreated, initialValue, initialFormattedValue) { }
    protected AFormattedValueComponent(T defaultValue, Observable<T> valueObservable, bool subscribeWhenCreated = false, Optional<T> initialValue = default, Optional<string> initialFormattedValue = default)
    {
        if (!subscribeWhenCreated)
        {
            Value = new BindableReactiveProperty<T>(value: initialValue.ValueOr(defaultValue));
            FormattedValue = new BindableReactiveProperty<string>(value: initialFormattedValue.ValueOr(alternativeValue: string.Empty));

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
            Value = valueObservable.ToBindableReactiveProperty(initialValue: initialValue.ValueOr(defaultValue));
            FormattedValue = Value.Select(this, static (value, self) => self.FormatValue(value)).ToBindableReactiveProperty(initialValue: initialFormattedValue.ValueOr(alternativeValue: string.Empty));
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
