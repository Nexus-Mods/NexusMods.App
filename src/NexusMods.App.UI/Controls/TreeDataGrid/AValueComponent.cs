using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for a single value.
/// </summary>
[PublicAPI]
public abstract class AValueComponent<T> : ReactiveR3Object, IItemModelComponent
{
    /// <summary>
    /// Gets the value property.
    /// </summary>
    public BindableReactiveProperty<T> Value { get; }

    private readonly IDisposable? _activationDisposable;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="initialValue">Initial value.</param>
    /// <param name="valueObservable">Observable.</param>
    /// <param name="subscribeWhenCreated">Whether to subscribe immediately when the component gets created or when the component gets activated.</param>
    protected AValueComponent(T initialValue, IObservable<T> valueObservable, bool subscribeWhenCreated = false) : this(initialValue, valueObservable.ToObservable(), subscribeWhenCreated) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="initialValue">Initial value.</param>
    /// <param name="valueObservable">Observable.</param>
    /// <param name="subscribeWhenCreated">Whether to subscribe immediately when the component gets created or when the component gets activated.</param>
    protected AValueComponent(T initialValue, Observable<T> valueObservable, bool subscribeWhenCreated = false)
    {
        if (!subscribeWhenCreated)
        {
            Value = new BindableReactiveProperty<T>(value: initialValue);

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
            Value = valueObservable.ToBindableReactiveProperty(initialValue: initialValue);
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
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

public class ValueComponent<T> : AValueComponent<T>
{
    public ValueComponent(
        T initialValue,
        IObservable<T> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    public ValueComponent(
        T initialValue,
        Observable<T> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    public ValueComponent(T value) : base(value) { }
}
