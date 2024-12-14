using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
    private readonly ObservableDictionary<string, IItemModelComponent> _components = new();
    public IReadOnlyObservableDictionary<string, IItemModelComponent> Components => _components;

    private readonly IDisposable _activationDisposable;
    public CompositeItemModel()
    {
        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            foreach (var kv in self._components)
            {
                var (_, component) = kv;
                if (component is not IReactiveR3Object reactiveR3Object) continue;
                reactiveR3Object.BetterActivate().AddTo(disposables);
            }

            self._components.ObserveChanged().ObserveOnUIThreadDispatcher().Subscribe((self, disposables), static (change, tuple) =>
            {
                var (_, disposables) = tuple;
                if (change.Action == NotifyCollectionChangedAction.Add)
                {
                    if (change.NewItem.Value is not IReactiveR3Object reactiveR3Object) return;
                    reactiveR3Object.BetterActivate().AddTo(disposables);
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

    public void Add(string key, IItemModelComponent component)
    {
        _components.Add(key, component);
    }

    public bool Remove(string key)
    {
        if (!_components.Remove(key, out var value)) return false;
        if (value is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return true;
    }

    public bool TryGet<TComponent>(string key, [NotNullWhen(true)] out TComponent? component)
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
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
