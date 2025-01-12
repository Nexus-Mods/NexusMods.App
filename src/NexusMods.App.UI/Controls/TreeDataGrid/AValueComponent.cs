using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public abstract class AValueComponent<T> : ReactiveR3Object, IItemModelComponent
    where T : notnull
{
    public BindableReactiveProperty<T> Value { get; }

    private readonly IDisposable? _activationDisposable;
    protected AValueComponent(T initialValue, IObservable<T> valueObservable, bool subscribeWhenCreated = false) : this(initialValue, valueObservable.ToObservable(), subscribeWhenCreated) { }
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
