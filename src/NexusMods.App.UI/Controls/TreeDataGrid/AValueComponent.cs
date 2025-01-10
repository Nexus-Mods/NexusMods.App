using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

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
