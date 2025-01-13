using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Item model that allows for composition over inheritance with components.
/// </summary>
[PublicAPI]
public class CompositeItemModel<TKey> : TreeDataGridItemModel<CompositeItemModel<TKey>, TKey>
    where TKey : notnull
{
    /// <summary>
    /// Gets the key of the item model.
    /// </summary>
    public TKey Key { get; }

    private readonly Dictionary<ComponentKey, IDisposable> _observableSubscriptions = new();
    private readonly ObservableDictionary<ComponentKey, IItemModelComponent> _components = new();

    /// <summary>
    /// Gets the dictionary of all components currently in the item model.
    /// </summary>
    public IReadOnlyObservableDictionary<ComponentKey, IItemModelComponent> Components => _components;

    private readonly IDisposable _activationDisposable;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CompositeItemModel(TKey key)
    {
        Key = key;

        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            foreach (var kv in self._components)
            {
                var (_, component) = kv;
                if (component is not IReactiveR3Object reactiveR3Object) continue;
                reactiveR3Object.Activate().AddTo(disposables);
            }

            self._components.ObserveDictionaryAdd().ObserveOnUIThreadDispatcher().Subscribe((self, disposables), static (change, tuple) =>
            {
                var (_, disposables) = tuple;
                if (change.Value is not IReactiveR3Object reactiveR3Object) return;
                reactiveR3Object.Activate().AddTo(disposables);
            }).AddTo(disposables);

            self._components.ObserveDictionaryRemove().ObserveOnUIThreadDispatcher().Subscribe((self, disposables), static (change, tuple) =>
            {
                var (_, disposables) = tuple;
                if (change.Value is not IReactiveR3Object reactiveR3Object) return;
                // NOTE(erri120): CompositeDisposable.Remove disposes the object
                var didRemove = disposables.Remove(reactiveR3Object);
                Debug.Assert(didRemove);
            }).AddTo(disposables);
        });
    }

    /// <summary>
    /// Adds the component with the given key to the item model.
    /// </summary>
    public void Add<TComponent>(ComponentKey key, IItemModelComponent<TComponent> component)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        _components.Add(key, component);
    }

    /// <summary>
    /// Remove the component with the given key from the item model.
    /// </summary>
    public bool Remove<TComponent>(ComponentKey key) where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        if (!_components.Remove(key, out var value)) return false;

        AssertComponent<TComponent>(key, value);
        if (value is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return true;
    }

    public delegate IItemModelComponent<TComponent> ComponentFactory<TComponent, T>(Observable<T> valueObservable, T initialValue)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>;

    /// <summary>
    /// Conditionally adds the component to the item model based on values from an observable stream.
    /// </summary>
    public void AddObservable<TComponent, T>(
        ComponentKey key,
        IObservable<Optional<T>> observable,
        ComponentFactory<TComponent, T> componentFactory,
        bool subscribeImmediately = false)
        where T : notnull
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        AddObservable(key, observable.ToObservable(), componentFactory, subscribeImmediately);
    }

    /// <summary>
    /// Conditionally adds the component to the item model based on values from an observable stream.
    /// </summary>
    public void AddObservable<TComponent, T>(
        ComponentKey key,
        Observable<Optional<T>> observable,
        ComponentFactory<TComponent, T> componentFactory,
        bool subscribeImmediately = false)
        where T : notnull
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        IDisposable disposable;
        if (subscribeImmediately)
        {
            disposable = SubscribeToObservable(key, observable, componentFactory);
        }
        else
        {
            // ReSharper disable once NotDisposedResource
            disposable = this.WhenActivated((key, observable, componentFactory), static (self, tuple, disposables) =>
            {
                var (key, observable, componentFactory) = tuple;
                self.SubscribeToObservable(key, observable, componentFactory).AddTo(disposables);
            });
        }

        if (_observableSubscriptions.Remove(key, out var existingDisposable))
        {
            existingDisposable.Dispose();
        }

        _observableSubscriptions.Add(key, disposable);
    }

    [MustUseReturnValue]
    private IDisposable SubscribeToObservable<TComponent, T>(
        ComponentKey key,
        Observable<Optional<T>> observable,
        ComponentFactory<TComponent, T> componentFactory)
        where T : notnull
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        return observable.Subscribe((this, key, observable, componentFactory), static (optionalValue, tuple) =>
        {
            var (self, key, observable, componentFactory) = tuple;

            if (self._components.ContainsKey(key))
            {
                if (optionalValue.HasValue) return;
                self.Remove<TComponent>(key);
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

    /// <summary>
    /// Tries to get the component with the given key.
    /// </summary>
    public bool TryGet<TComponent>(ComponentKey key, [NotNullWhen(true)] out TComponent? component)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        if (!_components.TryGetValue(key, out var value))
        {
            component = null;
            return false;
        }

        component = AssertComponent<TComponent>(key, value);
        return true;
    }

    /// <summary>
    /// Tries to get the component with the given key.
    /// </summary>
    public bool TryGet(ComponentKey key, Type componentType, [NotNullWhen(true)] out IItemModelComponent? component)
    {
        if (!_components.TryGetValue(key, out var value))
        {
            component = null;
            return false;
        }

        AssertComponent(key, componentType, value);

        component = value;
        return true;
    }

    /// <summary>
    /// Gets the component with the given key.
    /// </summary>
    public TComponent Get<TComponent>(ComponentKey key) where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        return AssertComponent<TComponent>(key, _components[key]);
    }

    /// <summary>
    /// Tries to get the component with the given key.
    /// </summary>
    public Optional<TComponent> GetOptional<TComponent>(ComponentKey key) where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        return TryGet<TComponent>(key, out var component) ? component : Optional<TComponent>.None;
    }

    /// <summary>
    /// Returns an observable stream with optional values when the component gets added or removed from the item model.
    /// </summary>
    /// <remarks>
    /// On subscription, the current value gets prepended.
    /// </remarks>
    public Observable<Optional<TComponent>> GetObservable<TComponent>(ComponentKey key) where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        var adds = _components
            .ObserveDictionaryAdd()
            .Where(key, static (ev, key) => ev.Key == key)
            .Select(key, static (ev, key) => Optional<TComponent>.Create(AssertComponent<TComponent>(key, ev.Value)));

        var removes = _components
            .ObserveDictionaryRemove()
            .Where(key, static (ev, key) => ev.Key == key)
            .Select(static _ => Optional<TComponent>.None);

        return adds.Merge(removes).Prepend((this, key), static state => state.Item1.GetOptional<TComponent>(state.Item2));
    }

    /// <summary>
    /// Subscribes to a component with the given key in the item model.
    /// </summary>
    public IDisposable SubscribeToComponent<TComponent, TState>(
        ComponentKey key,
        TState state,
        Func<TState, CompositeItemModel<TKey>, TComponent, IDisposable> factory)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        var observable = GetObservable<TComponent>(key);
        var serialDisposable = new SerialDisposable();

        var disposable = observable.Subscribe((this, state, factory, serialDisposable), static (optional, tuple) =>
        {
            var (self, state, factory, serialDisposable) = tuple;

            serialDisposable.Disposable = null;
            if (!optional.HasValue) return;

            serialDisposable.Disposable = factory(state, self, optional.Value);
        });

        return Disposable.Combine(serialDisposable, disposable);
    }

    private static TComponent AssertComponent<TComponent>(ComponentKey key, IItemModelComponent component)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        if (component is not TComponent actual)
            throw new InvalidOperationException($"Expected component with key `{key}` to be of type `{typeof(TComponent)}` but found `{component.GetType()}`");

        return actual;
    }

    private static void AssertComponent(ComponentKey key, Type type, IItemModelComponent component)
    {
        if (type.IsInstanceOfType(component)) return;
        throw new InvalidOperationException($"Expected component with key `{key}` to be of type `{type}` but found `{component.GetType()}`");
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
