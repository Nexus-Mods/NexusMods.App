using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for a single value and it's formatted string representation.
/// </summary>
[PublicAPI]
public abstract class AFormattedValueComponent<T> : ReactiveR3Object, IItemModelComponent
    where T : notnull
{
    /// <summary>
    /// Gets the value property.
    /// </summary>
    public BindableReactiveProperty<T> Value { get; }

    /// <summary>
    /// Gets the formatted value property.
    /// </summary>
    public BindableReactiveProperty<string> FormattedValue { get; }

    private readonly IDisposable? _activationDisposable;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="initialValue">Initial value.</param>
    /// <param name="initialFormattedValue">Initial value formatted.</param>
    /// <param name="valueObservable">Observable.</param>
    /// <param name="subscribeWhenCreated">Whether to subscribe immediately when the component gets created or when the component gets activated.</param>
    protected AFormattedValueComponent(T initialValue, string initialFormattedValue, IObservable<T> valueObservable, bool subscribeWhenCreated = false) : this(initialValue, initialFormattedValue, valueObservable.ToObservable(), subscribeWhenCreated) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="initialValue">Initial value.</param>
    /// <param name="initialFormattedValue">Initial value formatted.</param>
    /// <param name="valueObservable">Observable.</param>
    /// <param name="subscribeWhenCreated">Whether to subscribe immediately when the component gets created or when the component gets activated.</param>
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

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AFormattedValueComponent(T value, string formattedValue)
    {
        Value = new BindableReactiveProperty<T>(value: value);
        FormattedValue = new BindableReactiveProperty<string>(value: formattedValue);
    }

    /// <summary>
    /// Formats the given value to a localized string representation.
    /// </summary>
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
