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
    private readonly Dictionary<Type, IDisposable> _observableSubscriptions = new();
    private readonly ObservableDictionary<Type, IItemModelComponent> _components = new();
    public IReadOnlyObservableDictionary<Type, IItemModelComponent> Components => _components;

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

    public void Add<TComponent>(IItemModelComponent<TComponent> component)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        _components.Add(typeof(TComponent), component);
    }

    public void Remove<TComponent>() where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        if (_components.Remove(typeof(TComponent), out var value))
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public delegate IItemModelComponent<TComponent> ComponentFactory<TComponent, T>(Observable<T> valueObservable, T initialValue)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>;

    public void AddObservable<TComponent, T>(
        IObservable<Optional<T>> observable,
        ComponentFactory<TComponent, T> componentFactory,
        bool subscribeImmediately = false)
        where T : notnull
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        AddObservable(observable.ToObservable(), componentFactory, subscribeImmediately);
    }

    public void AddObservable<TComponent, T>(
        Observable<Optional<T>> observable,
        ComponentFactory<TComponent, T> componentFactory,
        bool subscribeImmediately = false)
        where T : notnull
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        IDisposable disposable;
        if (subscribeImmediately)
        {
            disposable = SubscribeToObservable(this, observable, componentFactory);
        }
        else
        {
            // ReSharper disable once NotDisposedResource
            disposable = this.WhenActivated((observable, componentFactory), static (self, tuple, disposables) =>
            {
                var (observable, componentFactory) = tuple;
                SubscribeToObservable(self, observable, componentFactory).AddTo(disposables);
            });
        }

        if (_observableSubscriptions.Remove(typeof(TComponent), out var existingDisposable))
        {
            existingDisposable.Dispose();
        }

        _observableSubscriptions.Add(typeof(TComponent), disposable);
    }

    private static IDisposable SubscribeToObservable<TComponent, T>(
        CompositeItemModel<TKey> self,
        Observable<Optional<T>> observable,
        ComponentFactory<TComponent, T> componentFactory)
        where T : notnull
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        return observable.Subscribe((self, observable, componentFactory), static (optionalValue, tuple) =>
        {
            var (self, observable, componentFactory) = tuple;

            if (self._components.ContainsKey(typeof(TComponent)))
            {
                if (optionalValue.HasValue) return;
                self.Remove<TComponent>();
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

                self.Add(component);
            }
        });
    }

    public bool TryGet<TComponent>([NotNullWhen(true)] out TComponent? component)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        if (!_components.TryGetValue(typeof(TComponent), out var value))
        {
            component = null;
            return false;
        }

        if (value is not TComponent typedValue)
        {
            component = null;
            return false;
        }

        component = typedValue;
        return true;
    }

    public TComponent Get<TComponent>() where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        if (!_components.TryGetValue(typeof(TComponent), out var value))
            throw new KeyNotFoundException($"Model doesn't have component `{typeof(TComponent)}`");

        if (value is not TComponent component)
            throw new InvalidCastException($"Expected component of type `{typeof(TComponent)}`, found `{value.GetType()}`");

        return component;
    }

    public Optional<TComponent> GetOptional<TComponent>() where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        return TryGet<TComponent>(out var component) ? component : Optional<TComponent>.None;
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
