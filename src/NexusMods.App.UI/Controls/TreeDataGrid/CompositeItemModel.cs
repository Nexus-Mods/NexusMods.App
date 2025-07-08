using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Converters;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Item model that allows for composition over inheritance with components.
/// </summary>
[PublicAPI]
public sealed class CompositeItemModel<TKey> : TreeDataGridItemModel<CompositeItemModel<TKey>, TKey>
    where TKey : notnull
{
    /// <summary>
    /// Gets the key of the item model.
    /// </summary>
    public TKey Key { get; }

    private readonly Dictionary<ComponentKey, IDisposable> _observableSubscriptions = new();
    private readonly ObservableDictionary<ComponentKey, IItemModelComponent> _components = new();
    private readonly CompositeDisposable _trackedDisposables = new();
    private readonly ObservableHashSet<string> _styleFlags = [];


    /// <summary>
    /// Gets the dictionary of all components currently in the item model.
    /// </summary>
    public IReadOnlyObservableDictionary<ComponentKey, IItemModelComponent> Components => _components;

    /// <summary>
    /// Gets the collection of style flags for the item model
    /// These can be set from Adapters based on components and component values
    /// Changes to the StyleFlags collection will raise PropertyChanged events to notify bindings
    /// See <see cref="CompositeStyleFlagConverter"/> for binding to the presence of a specific flag.
    /// </summary>
    public NotifyCollectionChangedSynchronizedViewList<string> StyleFlags { get; }


    private readonly IDisposable _activationDisposable;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CompositeItemModel(TKey key)
    {
        Key = key;
        StyleFlags = _styleFlags.ToNotifyCollectionChanged();

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

                switch (change.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        if (change.NewItem.Value is not IReactiveR3Object reactiveR3Object) return;
                        reactiveR3Object.Activate().AddTo(disposables);
                        break;
                    }
                    case NotifyCollectionChangedAction.Remove:
                    {
                        if (change.OldItem.Value is not IReactiveR3Object oldReactiveR3Object) return;
                        oldReactiveR3Object.Dispose();
                        break;
                    }
                    case NotifyCollectionChangedAction.Replace:
                    {
                        // Don't dispose the old item if it's the same as the new one.
                        if (ReferenceEquals(change.OldItem.Value, change.NewItem.Value)) return;
                        
                        if (change.OldItem.Value is IReactiveR3Object oldReactiveR3Object)
                        {
                            oldReactiveR3Object.Dispose();
                        }

                        if (change.NewItem.Value is not IReactiveR3Object reactiveR3Object) return;
                        reactiveR3Object.Activate().AddTo(disposables);
                        break;
                    }
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Reset:
                        return;
                    default: 
                        // ReSharper disable once LocalizableElement
                        throw new ArgumentOutOfRangeException(paramName: nameof(change.Action), actualValue: change.Action, "Unknown NotifyCollectionChangedAction");
                }
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

    public void AddObservable<TComponent>(
        ComponentKey key,
        Observable<bool> shouldAddObservable,
        Func<TComponent> componentFactory,
        bool subscribeImmediately = false)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        IDisposable disposable;
        if (subscribeImmediately)
        {
            disposable = SubscribeToObservable(key, shouldAddObservable, componentFactory);
        }
        else
        {
            // ReSharper disable once NotDisposedResource
            disposable = this.WhenActivated((key, shouldAddObservable, componentFactory), static (self, tuple, disposables) =>
            {
                var (key, shouldAddObservable, componentFactory) = tuple;
                self.SubscribeToObservable(key, shouldAddObservable, componentFactory).AddTo(disposables);
            });
        }

        if (_observableSubscriptions.Remove(key, out var existingDisposable))
        {
            existingDisposable.Dispose();
        }

        _observableSubscriptions.Add(key, disposable);
    }

    [MustUseReturnValue]
    private IDisposable SubscribeToObservable<TComponent>(
        ComponentKey key,
        Observable<bool> shouldAddObservable,
        Func<TComponent> componentFactory)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        return shouldAddObservable.ObserveOnUIThreadDispatcher().Subscribe((this, key, componentFactory), static (shouldAdd, tuple) =>
        {
            var (self, key, componentFactory) = tuple;

            if (self._components.ContainsKey(key))
            {
                if (shouldAdd) return;
                self.Remove<TComponent>(key);
            }
            else
            {
                if (!shouldAdd) return;
                var component = componentFactory();

                self.Add(key, component);
            }
        });
    }

    [MustUseReturnValue]
    private IDisposable SubscribeToObservable<TComponent, T>(
        ComponentKey key,
        Observable<Optional<T>> observable,
        ComponentFactory<TComponent, T> componentFactory)
        where T : notnull
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        return observable.ObserveOnUIThreadDispatcher().Subscribe((this, key, observable, componentFactory), static (optionalValue, tuple) =>
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
                        .Where(static optionalValue => optionalValue.HasValue)
                        .Select(static optionalValue => optionalValue.Value)
                        .ObserveOnUIThreadDispatcher(),
                    optionalValue.Value
                );

                self.Add(key, component);
            }
        });
    }
    
    /// <summary>
    /// Add a flat to the Composite Item Model, to be used in styling
    /// </summary>
    public void SetStyleFlag(string flag, bool value)
    {
        var modified = false;
        if (value)
        {
            modified = _styleFlags.Add(flag);
        }
        else
        {
            modified = _styleFlags.Remove(flag);
        }

        if (modified)
        {
            // Note(Al12rs): We need UI bindings to StyleFlags to be notified when the contents of the collection change
            RaisePropertyChanged(new PropertyChangedEventArgs(nameof(StyleFlags)));
        } 
    }

    /// <summary>
    /// Returns true if any of the children matches the given filter, false otherwise.
    /// </summary>
    /// <remarks>
    /// This method currently only works correctly if the item is expanded, otherwise the children data might not be available.
    /// </remarks>
    public bool ChildrenMatchFilter(Filter filter)
    {
        // NOTE(Al12rs): the contents of children are not guaranteed to be accurate.
        // The children observable is only subscribed while the parent is expanded, 
        // meaning that there is no way to get the children models data while the parent is collapsed. 
        // Trying to access <see cref="Children"/> would trigger the subscription, 
        // which would then keep the children models in memory and cause issues with activation and deactivation.
        // So at the moment, filtering only considers child items correctly if the parent is expanded, which sucks.
        foreach (var childModel in _children)
        {
            if (filter.MatchesRow(childModel))
            {
                return true; // If any child matches, the filter matches
            }
        }

        return false; // If no children matched, the filter fails
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
        var changes = _components
            .ObserveChanged()
            .Where(key,
                static (ev, key) => ev.Action switch
                        {
                            NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace => ev.NewItem.Key == key,
                            NotifyCollectionChangedAction.Remove => ev.OldItem.Key == key,
                            NotifyCollectionChangedAction.Reset => true,
                            NotifyCollectionChangedAction.Move => false,
                        }
            )
            .Select(key, static (ev, key) => 
                ev.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace ? 
                Optional<TComponent>.Create(AssertComponent<TComponent>(key, ev.NewItem.Value)) : 
                Optional<TComponent>.None
            );

        return changes.Prepend((this, key), static state => state.Item1.GetOptional<TComponent>(state.Item2));
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

    /// <summary>
    /// Subscribes to a component with the given key in the item model.
    /// </summary>
    /// <remarks>
    /// Differs from <see cref="SubscribeToComponent{TComponent,TState}"/> in that the
    /// disposable is tracked by the model itself.
    /// </remarks>
    public void SubscribeToComponentAndTrack<TComponent, TState>(
        ComponentKey key,
        TState state,
        Func<TState, CompositeItemModel<TKey>, TComponent, IDisposable> factory)
        where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
    {
        SubscribeToComponent(key, state, factory).AddTo(_trackedDisposables);
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
            _isDisposed = true;

            if (disposing)
            {
                _trackedDisposables.Dispose();
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
        }

        base.Dispose(disposing);
    }
}
