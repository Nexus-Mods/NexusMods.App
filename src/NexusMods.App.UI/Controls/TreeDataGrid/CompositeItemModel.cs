using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public class CompositeItemModel<TKey> :
    TreeDataGridItemModel<CompositeItemModel<TKey>, TKey>
    where TKey : notnull
{
    private readonly Dictionary<ComponentKey, IDisposable> _observableSubscriptions = new();
    private readonly ObservableDictionary<ComponentKey, IItemModelComponent> _components = new();
    public IReadOnlyObservableDictionary<ComponentKey, IItemModelComponent> Components => _components;

    private readonly IDisposable _activationDisposable;
    public CompositeItemModel()
    {
        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            foreach (var kv in self._components)
            {
                var (_, component) = kv;
                if (component is not IReactiveR3Object reactiveR3Object) continue;
                reactiveR3Object.Activate().AddTo(disposables);
            }

            self._components.ObserveChanged().ObserveOnUIThreadDispatcher().Subscribe((self, disposables), static (change, tuple) =>
            {
                var (_, disposables) = tuple;
                if (change.Action == NotifyCollectionChangedAction.Add)
                {
                    if (change.NewItem.Value is not IReactiveR3Object reactiveR3Object) return;
                    reactiveR3Object.Activate().AddTo(disposables);
                } else if (change.Action == NotifyCollectionChangedAction.Remove)
                {
                    if (change.OldItem.Value is not IReactiveR3Object reactiveR3Object) return;
                    reactiveR3Object.Dispose();
                } else if (change.Action == NotifyCollectionChangedAction.Reset)
                {
                    throw new NotSupportedException("Resets are not supported");
                }
            }).AddTo(disposables);
        });
    }

    public void Add(ComponentKey key, IItemModelComponent component)
    {
        _components.Add(key, component);
    }

    public delegate IItemModelComponent ComponentFactory<T>(Observable<T> valueObservable, T initialValue);

    public void AddObservable<T>(
        ComponentKey key,
        IObservable<Optional<T>> observable,
        ComponentFactory<T> componentFactory,
        bool subscribeImmediately = false) where T : notnull
    {
        AddObservable(key, observable.ToObservable(), componentFactory, subscribeImmediately);
    }

    public void AddObservable<T>(
        ComponentKey key,
        Observable<Optional<T>> observable,
        ComponentFactory<T> componentFactory,
        bool subscribeImmediately = false) where T : notnull
    {
        IDisposable disposable;
        if (subscribeImmediately)
        {
            disposable = SubscribeToObservable(this, key, observable, componentFactory);
        }
        else
        {
            // ReSharper disable once NotDisposedResource
            disposable = this.WhenActivated((key, observable, componentFactory), static (self, tuple, disposables) =>
            {
                var (key, observable, componentFactory) = tuple;
                SubscribeToObservable(self, key, observable, componentFactory).AddTo(disposables);
            });
        }

        if (_observableSubscriptions.TryGetValue(key, out var existingDisposable))
        {
            existingDisposable.Dispose();
        } else {
            _observableSubscriptions.Add(key, disposable);
        }
    }

    private static IDisposable SubscribeToObservable<T>(
        CompositeItemModel<TKey> self,
        ComponentKey key,
        Observable<Optional<T>> observable,
        ComponentFactory<T> componentFactory) where T : notnull
    {
        return observable.Subscribe((self, key, observable, componentFactory), static (optionalValue, tuple) =>
        {
            var (self, key, observable, componentFactory) = tuple;
            if (self._components.ContainsKey(key))
            {
                if (optionalValue.HasValue) return;
                self.Remove(key: key);
            }
            else
            {
                if (!optionalValue.HasValue) return;
                var component = componentFactory(
                    observable
                        .ObserveOnUIThreadDispatcher()
                        .Where(static optionalValue => optionalValue.HasValue)
                        .Select(static optionalValue => optionalValue.Value),
                    optionalValue.Value
                );

                self.Add(key, component);
            }
        });
    }

    public void Remove(ComponentKey key)
    {
        if (_components.Remove(key, out var value))
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        if (_observableSubscriptions.Remove(key, out var observableSubscription))
        {
            observableSubscription.Dispose();
        }
    }

    public bool TryGet<TComponent>(ComponentKey key, [NotNullWhen(true)] out TComponent? component)
    {
        if (!_components.TryGetValue(key, out var value))
        {
            component = default(TComponent);
            return false;
        }

        if (value is not TComponent typedValue)
        {
            component = default(TComponent);
            return false;
        }

        component = typedValue;
        return true;
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _activationDisposable.Dispose();

                foreach (var kv in _observableSubscriptions)
                {
                    kv.Value.Dispose();
                }

                foreach (var kv in _components)
                {
                    if (kv.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
