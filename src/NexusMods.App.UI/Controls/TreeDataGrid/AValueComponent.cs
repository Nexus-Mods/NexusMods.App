using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
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
    /// <param name="observeOutsideUiThread">Observes outside of the UI thread. Debug only, eliminated by the JIT in release.</param>
    protected AValueComponent(T initialValue, IObservable<T> valueObservable, bool subscribeWhenCreated = false, bool observeOutsideUiThread = false) : this(initialValue, valueObservable.ToObservable(), subscribeWhenCreated, observeOutsideUiThread) { }

    /// <summary>
    /// Returns the result of applying the given filter to this component's value
    /// </summary>
    public FilterResult MatchesFilter(Filter filter)
    {
        return filter.Match(Value.Value);
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="initialValue">Initial value.</param>
    /// <param name="valueObservable">Observable.</param>
    /// <param name="subscribeWhenCreated">Whether to subscribe immediately when the component gets created or when the component gets activated.</param>
    /// <param name="observeOutsideUiThread">Observes outside of the UI thread. Debug only, eliminated by the JIT in release.</param>
    protected AValueComponent(T initialValue, Observable<T> valueObservable, bool subscribeWhenCreated = false, bool observeOutsideUiThread = false)
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
            Value = !observeOutsideUiThread 
                ? valueObservable.ObserveOnUIThreadDispatcher().ToBindableReactiveProperty(initialValue: initialValue) 
                : valueObservable.ToBindableReactiveProperty(initialValue: initialValue); // Testing/Debug only.
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

public class ValueComponent<T> : AValueComponent<T>, IItemModelComponent<ValueComponent<T>>, IComparable<ValueComponent<T>>
{
    private readonly IComparer<T> _comparer;

    // System Observable variant
    public ValueComponent(
        T initialValue,
        IObservable<T> valueObservable,
        bool subscribeWhenCreated = false,
        IComparer<T>? comparer = null,
        bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated, observeOutsideUiThread)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }
    
    // R3 Observable variant
    public ValueComponent(
        T initialValue,
        R3.Observable<T> valueObservable,
        bool subscribeWhenCreated = false,
        IComparer<T>? comparer = null,
        bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated, observeOutsideUiThread)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }

    public ValueComponent(T value, IComparer<T>? comparer = null) : base(value)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }
    
    public int CompareTo(ValueComponent<T>? other)
    {
        if (other is null) return 1;
        return _comparer.Compare(Value.Value, other.Value.Value);
    }
}

