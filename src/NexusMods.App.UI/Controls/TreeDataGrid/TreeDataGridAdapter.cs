using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using DynamicData;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Extensions;
using ObservableCollections;
using R3;
using System.Reactive.Linq;
using Avalonia.Input;
using DynamicData.Kernel;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Adapter class for working with <see cref="TreeDataGrid"/>.
/// </summary>
public abstract class TreeDataGridAdapter<TModel, TKey> : ReactiveR3Object
    where TModel : class, ITreeDataGridItemModel<TModel, TKey>
    where TKey : notnull
{
    public Subject<(TModel model, bool isActivating)> ModelActivationSubject { get; } = new();
    
    public Subject<(TModel[] sourceModels, TreeDataGridRowDragStartedEventArgs e)> RowDragStartedSubject { get; } = new();
    public Subject<(TModel[] sourceModels, TModel target, TreeDataGridRowDragEventArgs e)> RowDragOverSubject { get; } = new();
    public Subject<(TModel[] sourceModels, TModel target, TreeDataGridRowDragEventArgs e)> RowDropSubject { get; } = new();

    public BindableReactiveProperty<ITreeDataGridSource<TModel>> Source { get; } = new();
    public BindableReactiveProperty<bool> ViewHierarchical { get; } = new(value: true);
    public BindableReactiveProperty<bool> IsSourceEmpty { get; } = new(value: true);
    public BindableReactiveProperty<int> SourceCount { get; } = new(value: 0);

    public ObservableHashSet<TModel> SelectedModels { get; private set; } = [];
    protected ObservableList<TModel> Roots { get; private set; } = [];
    private ISynchronizedView<TModel, TModel> RootsView { get; }
    private INotifyCollectionChangedSynchronizedViewList<TModel> RootsCollectionChangedView { get; }

    private readonly IDisposable _activationDisposable;
    private readonly SerialDisposable _selectionModelsSerialDisposable = new();
    protected TreeDataGridAdapter()
    {
        RootsView = Roots.CreateView(static kv => kv);
        RootsCollectionChangedView = RootsView.ToNotifyCollectionChanged();

        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            self.Roots
                .ObserveCountChanged(notifyCurrentCount: true)
                .Subscribe(self, static (count, self) =>
                {
                    self.SourceCount.Value = count;
                    self.IsSourceEmpty.Value = count == 0;
                })
                .AddTo(disposables);

            self.ModelActivationSubject.Subscribe(self, static (input, self) =>
            {
                var (model, isActivating) = input;

                // NOTE(erri120): This is only necessary for child rows since root rows are handled directly.
                if (isActivating && !model.IsActivated)
                {
                    self.BeforeModelActivationHook(model);
                    model.Activate();
                }
            }).AddTo(disposables);

            self.ViewHierarchical
                .AsObservable()
                .ObserveOnUIThreadDispatcher()
                .Do(self, static (viewHierarchical, self) =>
                {
                    foreach (var root in self.Roots)
                    {
                        root.Dispose();
                    }

                    self.Roots.Clear();

                    // NOTE(erri120): we have to do this manually, the TreeDataGrid doesn't deselect when changing source
                    self.SelectedModels.Clear();

                    var (source, selectionObservable) = self.CreateSource(self.RootsCollectionChangedView, createHierarchicalSource: viewHierarchical);

                    self._selectionModelsSerialDisposable.Disposable = selectionObservable.Subscribe(self, static (eventArgs, self) =>
                    {
                        self.SelectedModels.RemoveRange(eventArgs.DeselectedItems.NotNull());
                        foreach (var item in eventArgs.DeselectedItems)
                        {
                            if (item is null) continue;
                            item.IsSelected.Value = false;
                        }

                        self.SelectedModels.AddRange(eventArgs.SelectedItems.NotNull());
                        foreach (var item in eventArgs.SelectedItems)
                        {
                            if (item is null) continue;
                            item.IsSelected.Value = true;
                        }
                    });

                    self.Source.Value = source;
                })
                .Select(self, static (viewHierarchical, self) =>
                {
                    return self
                        .GetRootsObservable(viewHierarchical)
                        .OnUI()
                        .Do(changeSet =>
                        {
                            // NOTE(erri120): We activate all items before adding them to Roots
                            // so they get values before the TreeDataGrid even sees them.
                            foreach (var change in changeSet)
                            {
                                if (change.Reason is ChangeReason.Add)
                                {
                                    self.BeforeModelActivationHook(change.Current);
                                    change.Current.Activate();
                                }
                            }

                            self.Roots.ApplyChanges(changeSet);
                        })
                        .DisposeMany()
                        .ToObservable()
                        .Select(viewHierarchical, static (_, viewHierarchical) => viewHierarchical);
                })
                .Switch()
                .Subscribe()
                .AddTo(disposables);

            Disposable.Create(self._selectionModelsSerialDisposable, static serialDisposable => serialDisposable.Disposable = null).AddTo(disposables);

            Disposable.Create(self, static self =>
            {
                foreach (var root in self.Roots)
                {
                    root.Dispose();
                }

                self.Roots.Clear();
            }).AddTo(disposables);
        });
    }

    /// <summary>
    /// Called when a row drag operation is started.
    /// This is only called if <see cref="TreeDataGridViewHelper.SetupTreeDataGridAdapter"/> enableDragAndDrop parameter is set to true.
    /// </summary>
    public virtual void OnRowDragStarted(object? sender, TreeDataGridRowDragStartedEventArgs e)
    {
        var sourceModels = e.Models.OfType<TModel>().ToArray();
        if (sourceModels.Length == 0)
        {
            return;
        }
        RowDragStartedSubject.OnNext((sourceModels, e));
    }
    
    public virtual void OnRowDragOver(object? sender, TreeDataGridRowDragEventArgs e)
    {
        // extract the target model from the event args
        if (e.TargetRow.Model is not TModel targetModel) return;
        
        // extract the source models from the event args
        var dataObject = e.Inner.Data as DataObject;
        if (dataObject?.Get("TreeDataGridDragInfo") is not DragInfo dragInfo) return;

        var source = dragInfo.Source;
        var indices = dragInfo.Indexes;
        
        var sourceModels = new List<TModel>();
        
        foreach (var modelIndex in indices)
        {
            var rowIndex = source.Rows.ModelIndexToRowIndex(modelIndex);
            var row = source.Rows[rowIndex];
            if (row.Model is not TModel model) continue;
            sourceModels.Add(model);
        }

        RowDragOverSubject.OnNext((sourceModels.ToArray(), targetModel, e));
    }
    
    /// <summary>
    /// Called when one or more dragged rows are dropped.
    /// This is only called if <see cref="TreeDataGridViewHelper.SetupTreeDataGridAdapter"/> enableDragAndDrop parameter is set to true.
    /// </summary>
    public virtual void OnRowDrop(object? sender, TreeDataGridRowDragEventArgs e)
    {
        // NOTE(Al12rs): This is important in case the source is read-only, otherwise TreeDataGrid will attempt to
        // move the items, updating the source collection, throwing an exception in the process.
        e.Handled = true;
        
        // extract the target model from the event args
        if (e.TargetRow.Model is not TModel targetModel) return;
        
        // extract the source models from the event args
        var dataObject = e.Inner.Data as DataObject;
        if (dataObject?.Get("TreeDataGridDragInfo") is not DragInfo dragInfo) return;

        var source = dragInfo.Source;
        var indices = dragInfo.Indexes;
        
        var sourceModels = new List<TModel>();
        
        foreach (var modelIndex in indices)
        {
            var rowIndex = source.Rows.ModelIndexToRowIndex(modelIndex);
            var row = source.Rows[rowIndex];
            if (row.Model is not TModel model) continue;
            sourceModels.Add(model);
        }
        
        RowDropSubject.OnNext((sourceModels.ToArray(), targetModel, e));
    }

    private static (ITreeDataGridSelection, Observable<TreeSelectionModelSelectionChangedEventArgs<TModel>>) CreateSelection(ITreeDataGridSource<TModel> source)
    {
        var selection = new TreeDataGridRowSelectionModel<TModel>(source)
        {
            SingleSelect = false,
        };

        var selectionObservable = R3.Observable.FromEventHandler<TreeSelectionModelSelectionChangedEventArgs<TModel>>(
            addHandler: handler => selection.SelectionChanged += handler,
            removeHandler: handler => selection.SelectionChanged -= handler
        ).Select(tuple => tuple.e);

        return (selection, selectionObservable);
    }

    private (ITreeDataGridSource<TModel> source, Observable<TreeSelectionModelSelectionChangedEventArgs<TModel>> selectionObservable) CreateSource(IEnumerable<TModel> models, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<TModel>(models);
            var (selection, selectionObservable) = CreateSelection(source);
            source.Selection = selection;

            source.Columns.AddRange(CreateColumns(viewHierarchical: createHierarchicalSource));
            return (source, selectionObservable);
        }
        else
        {
            var source = new FlatTreeDataGridSource<TModel>(models);
            var (selection, selectionObservable) = CreateSelection(source);
            source.Selection = selection;

            source.Columns.AddRange(CreateColumns(viewHierarchical: createHierarchicalSource));
            return (source, selectionObservable);
        }
    }

    protected virtual void BeforeModelActivationHook(TModel model) {}

    protected abstract IObservable<IChangeSet<TModel, TKey>> GetRootsObservable(bool viewHierarchical);
    protected abstract IColumn<TModel>[] CreateColumns(bool viewHierarchical);

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(_activationDisposable, RootsView, RootsCollectionChangedView, _selectionModelsSerialDisposable, ModelActivationSubject);
            }

            Roots = null!;
            SelectedModels = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
