using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = System.Reactive.Linq.Observable;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Base class for models of <see cref="Avalonia.Controls.TreeDataGrid"/> items.
/// </summary>
public class TreeDataGridItemModel : ReactiveObject, IDisposable
{
    private readonly BehaviorSubject<bool> _activation = new(initialValue: false);
    public Observable<bool> Activation => _activation;

    internal void Activate() => _activation.OnNext(true);
    internal void Deactivate() => _activation.OnNext(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private bool _isDisposed;

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _activation.Dispose();
        }

        _isDisposed = true;
    }
}

/// <summary>
/// Generic variant of <see cref="TreeDataGridItemModel"/>.
/// </summary>
/// <typeparam name="TModel"></typeparam>
[PublicAPI]
public class TreeDataGridItemModel<TModel> : TreeDataGridItemModel
    where TModel : TreeDataGridItemModel<TModel>
{
    public IObservable<bool> HasChildrenObservable { get; init; } = Observable.Return(false);
    [Reactive] public bool HasChildren { get; private set; }

    public IObservable<IChangeSet<TModel>> ChildrenObservable { get; init; } = Observable.Empty<IChangeSet<TModel>>();
    private ObservableCollectionExtended<TModel> _children = [];

    private readonly BehaviorSubject<bool> _childrenActivation = new(initialValue: false);

    [DebuggerBrowsable(state: DebuggerBrowsableState.Never)]
    public ObservableCollectionExtended<TModel> Children
    {
        get
        {
            _childrenActivation.OnNext(true);
            return _children;
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded && !value) _childrenActivation.OnNext(false);
            this.RaiseAndSetIfChanged(ref _isExpanded, value);
        }
    }

    private readonly IDisposable _modelActivationDisposable;
    protected TreeDataGridItemModel()
    {
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            // NOTE(erri120): TreeDataGrid uses `HasChildren` to show/hide the expander.
            model.HasChildrenObservable
                .OnUI()
                .SubscribeWithErrorLogging(hasChildren => model.HasChildren = hasChildren)
                .AddTo(disposables);

            var serialDisposable = new SerialDisposable();
            serialDisposable.AddTo(disposables);

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
            model._childrenActivation
                .DistinctUntilChanged()
                .Subscribe((model, serialDisposable), static (isActivated, state) =>
                {
                    var (model, serialDisposable) = state;

                    // NOTE(erri120): We always need to reset when the observable triggers.
                    // Note that the observable we're currently in with the `DistinctUntilChanged`
                    // gets disposed when the model is deactivated. This is important to
                    // understand for the model and child activation/deactivation relationships.
                    serialDisposable.Disposable = null;
                    CleanupChildren(model._children);

                    if (isActivated)
                    {
                        serialDisposable.Disposable = model.ChildrenObservable
                            .OnUI()
                            .Bind(model._children)
                            .SubscribeWithErrorLogging();
                    }
                })
                .AddTo(disposables);
        });
    }

    private static void CleanupChildren(ObservableCollectionExtended<TModel> children)
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
                Disposable.Dispose(_childrenActivation, _modelActivationDisposable);
            }

            _children = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    [MustDisposeResource] protected static IDisposable WhenModelActivated<TItemModel>(TItemModel model, Action<TItemModel, CompositeDisposable> block)
        where TItemModel : TreeDataGridItemModel
    {
        var d = Disposable.CreateBuilder();

        var serialDisposable = new SerialDisposable();
        serialDisposable.AddTo(ref d);

        model.Activation.DistinctUntilChanged().Subscribe((model, serialDisposable, block), onNext: static (isActivated, state) =>
        {
            var (model, serialDisposable, block) = state;

            serialDisposable.Disposable = null;
            if (isActivated)
            {
                var compositeDisposable = new CompositeDisposable();
                serialDisposable.Disposable = compositeDisposable;

                block(model, compositeDisposable);
            }
        }, onCompleted: static (_, state) =>
        {
            var (_, serialDisposable, _) = state;
            serialDisposable.Disposable = null;
        }).AddTo(ref d);

        return d.Build();
    }

    public static HierarchicalExpanderColumn<TModel> CreateExpanderColumn(IColumn<TModel> innerColumn)
    {
        return new HierarchicalExpanderColumn<TModel>(
            inner: innerColumn,
            childSelector: static model => model.Children,
            hasChildrenSelector: static model => model.HasChildren,
            isExpandedSelector: static model => model.IsExpanded
        )
        {
            Tag = "expander",
        };
    }
}
