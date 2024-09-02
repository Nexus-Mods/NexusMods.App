using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using DynamicData;
using NexusMods.App.UI.Extensions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Adapter class for working with <see cref="TreeDataGrid"/>.
/// </summary>
public abstract class TreeDataGridAdapter<TModel, TKey> : ReactiveR3Object
    where TModel : TreeDataGridItemModel<TModel, TKey>
    where TKey : notnull
{
    public Subject<(TModel model, bool isActivating)> ModelActivationSubject { get; } = new();

    public BindableReactiveProperty<ITreeDataGridSource<TModel>> Source { get; } = new();
    public BindableReactiveProperty<bool> ViewHierarchical { get; } = new(value: true);
    public BindableReactiveProperty<bool> IsSourceEmpty { get; } = new(value: true);

    public ObservableList<TModel> SelectedModels { get; private set; } = [];

    private ObservableList<TModel> Roots { get; set; } = [];
    private ISynchronizedView<TModel, TModel> RootsView { get; }
    private INotifyCollectionChangedSynchronizedView<TModel> RootsCollectionChangedView { get; }

    private readonly IDisposable _activationDisposable;
    private readonly SerialDisposable _selectionModelsSerialDisposable = new();
    protected TreeDataGridAdapter()
    {
        RootsView = Roots.CreateView(static kv => kv);
        RootsCollectionChangedView = RootsView.ToNotifyCollectionChanged();

        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            self.ModelActivationSubject
                .Subscribe(self, static (tuple, self) =>
                {
                    var (model, isActivating) = tuple;

                    if (isActivating)
                    {
                        self.BeforeModelActivationHook(model);
                        model.Activate();
                    } else
                    {
                        self.BeforeModelDeactivationHook(model);
                        model.Deactivate();
                    }
                })
                .AddTo(disposables);

            self.Roots
                .ObserveCountChanged()
                .Subscribe(self, static (count, self) => self.IsSourceEmpty.Value = count == 0)
                .AddTo(disposables);

            self.ViewHierarchical
                .AsObservable()
                .Select(self, static (viewHierarchical, self) =>
                {
                    self.Roots.Clear();

                    // NOTE(erri120): we have to do this manually, the TreeDataGrid doesn't deselect when changing source
                    self.SelectedModels.Clear();

                    return self
                        .GetRootsObservable(viewHierarchical)
                        .OnUI()
                        .DisposeMany()
                        .ToObservable()
                        .Do(self, static (changeSet, self) => self.Roots.ApplyChanges(changeSet))
                        .Select(viewHierarchical, static (_, viewHierarchical) => viewHierarchical);
                })
                .Switch()
                .Select(self, static (viewHierarchical, self) => self.CreateSource(self.RootsCollectionChangedView, createHierarchicalSource: viewHierarchical))
                .Subscribe(self, static (tuple, self) =>
                {
                    self._selectionModelsSerialDisposable.Disposable = tuple.selectionObservable.Subscribe(self, static (eventArgs, self) =>
                    {
                        self.SelectedModels.Remove(eventArgs.DeselectedItems.NotNull());
                        self.SelectedModels.AddRange(eventArgs.SelectedItems.NotNull());
                    });

                    self.Source.Value = tuple.source;
                }).AddTo(disposables);

            Disposable.Create(self._selectionModelsSerialDisposable, static serialDisposable => serialDisposable.Disposable = null).AddTo(disposables);
        });
    }

    private static (ITreeDataGridSelection, Observable<TreeSelectionModelSelectionChangedEventArgs<TModel>>) CreateSelection(ITreeDataGridSource<TModel> source)
    {
        var selection = new TreeDataGridRowSelectionModel<TModel>(source)
        {
            SingleSelect = false,
        };

        var selectionObservable = Observable.FromEventHandler<TreeSelectionModelSelectionChangedEventArgs<TModel>>(
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
    protected virtual void BeforeModelDeactivationHook(TModel model) {}

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
