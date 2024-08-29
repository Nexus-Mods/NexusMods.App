using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;
using ObservableCollections;
using R3;
using Observable = System.Reactive.Linq.Observable;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Base class for models of <see cref="Avalonia.Controls.TreeDataGrid"/> items.
/// </summary>
public class TreeDataGridItemModel : ReactiveR3Object;

/// <summary>
/// Generic variant of <see cref="TreeDataGridItemModel"/>.
/// </summary>
[PublicAPI]
public class TreeDataGridItemModel<TModel, TKey> : TreeDataGridItemModel
    where TModel : TreeDataGridItemModel<TModel, TKey>
    where TKey : notnull
{
    public IObservable<bool> HasChildrenObservable { get; init; } = Observable.Return(false);
    public BindableReactiveProperty<bool> HasChildren { get; } = new();

    public IObservable<IChangeSet<TModel, TKey>> ChildrenObservable { get; init; } = Observable.Empty<IChangeSet<TModel, TKey>>();
    private ObservableList<TModel> _children = [];
    private readonly INotifyCollectionChangedSynchronizedView<TModel> _childrenView;

    private readonly BehaviorSubject<bool> _childrenCollectionInitialization = new(initialValue: false);

    [DebuggerBrowsable(state: DebuggerBrowsableState.Never)]
    public IEnumerable<TModel> Children
    {
        get
        {
            _childrenCollectionInitialization.OnNext(true);
            return _childrenView;
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded && !value) _childrenCollectionInitialization.OnNext(false);
            RaiseAndSetIfChanged(ref _isExpanded, value);
        }
    }

    private readonly IDisposable _modelActivationDisposable;

    private readonly SerialDisposable _childrenCollectionInitializationSerialDisposable = new();
    private readonly SerialDisposable _childrenObservableSerialDisposable = new();

    protected TreeDataGridItemModel()
    {
        _childrenView = _children.CreateView(static model => model).ToNotifyCollectionChanged();

        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            // NOTE(erri120): TreeDataGrid uses `HasChildren` to show/hide the expander.
            model.HasChildrenObservable
                .OnUI()
                .SubscribeWithErrorLogging(hasChildren => model.HasChildren.Value = hasChildren)
                .AddTo(disposables);

            // NOTE(erri120): We only do this once. If you have an expanded parent and scroll
            // past it, we don't want the subscription to be disposed.
            if (model._childrenCollectionInitializationSerialDisposable.Disposable is null)
            {
                // NOTE(erri120): TreeDataGrid accesses the `Children` collection only when
                // the user opens the expander. The order is as follows:
                // 1) TreeDataGrid checks `HasChildren` to show/hide the expander.
                // 2) The user clicks on the visible expander icon.
                // 3) TreeDataGrid checks `Children` first, since `HasChildren` and the `Children` collection
                //    can be out-of-sync, TreeDataGrid will check if there are any children and possibly
                //    hides the expander even if `HasChildren` is `true`.
                // 4) If `Children` is non-empty, TreeDataGrid sets `IsExpanded` to `true` and adds rows to the tree.
                // To get a lazy-initialized collection, we use the `Children` getter to introduce a side effect
                // that will activate the subject below.
                // Since this ordering is depended on the TreeDataGrid implementation, it's not very robust and
                // mostly out of our control.
                // Additionally, subscribing to `ChildrenObservable` has to return at least one item immediately,
                // otherwise we return an empty collection.
                model._childrenCollectionInitializationSerialDisposable.Disposable = model._childrenCollectionInitialization
                    .DistinctUntilChanged()
                    .Subscribe(model, onNext: static (isInitializing, model) =>
                    {
                        // NOTE(erri120): We always need to reset when the observable triggers.
                        // Note that the observable we're currently in with the `DistinctUntilChanged`
                        // gets disposed when the model is deactivated. This is important to
                        // understand for the model and child activation/deactivation relationships.
                        model._childrenObservableSerialDisposable.Disposable = null;
                        CleanupChildren(model._children);

                        if (isInitializing)
                        {
                            model._childrenObservableSerialDisposable.Disposable = model.ChildrenObservable
                                .OnUI()
                                .SubscribeWithErrorLogging(changeSet => model._children.ApplyChanges(changeSet));
                        }
                    }, onCompleted: static (_, model) => CleanupChildren(model._children));
            }
        });
    }

    private static void CleanupChildren(ObservableList<TModel> children)
    {
        foreach (var child in children)
        {
            child.Dispose();
        }

        children.Clear();
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(
                    _childrenCollectionInitialization,
                    _modelActivationDisposable,
                    _childrenObservableSerialDisposable,
                    _childrenCollectionInitializationSerialDisposable,
                    HasChildren
                );
            }

            _children = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    [MustDisposeResource] protected static IDisposable WhenModelActivated<TItemModel>(TItemModel model, Action<TItemModel, CompositeDisposable> block)
        where TItemModel : TreeDataGridItemModel
    {
        return model.WhenActivated(block);
    }

    public static HierarchicalExpanderColumn<TModel> CreateExpanderColumn(IColumn<TModel> innerColumn)
    {
        return new HierarchicalExpanderColumn<TModel>(
            inner: innerColumn,
            childSelector: static model => model.Children,
            hasChildrenSelector: static model => model.HasChildren.Value,
            isExpandedSelector: static model => model.IsExpanded
        )
        {
            Tag = "expander",
        };
    }
}
