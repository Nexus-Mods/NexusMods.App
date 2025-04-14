using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using R3;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using ReactiveCommand = ReactiveUI.ReactiveCommand;
using Unit = System.Reactive.Unit;
using OneOf;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public string SortOrderName { get; }
    public string InfoAlertTitle { get; }
    public string InfoAlertBody { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> ToggleAlertCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> LearnMoreAlertCommand { get; }
    public string TrophyToolTip { get; }
    [Reactive] public ListSortDirection SortDirectionCurrent { get; set; }
    
    public ReactiveUI.ReactiveCommand<Unit, Unit> SwitchSortDirectionCommand { get; }
    
    [Reactive] public bool IsAscending { get; private set; }
    [Reactive] public bool IsWinnerTop { get; private set; }
    public string EmptyStateMessageTitle { get; }
    public string EmptyStateMessageContents { get; }

    public AlertSettingsWrapper AlertSettingsWrapper { get; }

    public TreeDataGridAdapter<CompositeItemModel<Guid>, Guid> Adapter { get; }

    public LoadOrderViewModel(LoadoutId loadoutId, 
        ISortableItemProviderFactory itemProviderFactory, 
        IServiceProvider serviceProvider,
        IOSInterop osInterop)
    {
        var provider = itemProviderFactory.GetLoadoutSortableItemProvider(loadoutId);
        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();

        SortOrderName = itemProviderFactory.SortOrderName;
        InfoAlertTitle = itemProviderFactory.OverrideInfoTitle;
        InfoAlertBody = itemProviderFactory.OverrideInfoMessage;
        TrophyToolTip = itemProviderFactory.WinnerIndexToolTip;
        EmptyStateMessageTitle = itemProviderFactory.EmptyStateMessageTitle;
        EmptyStateMessageContents = itemProviderFactory.EmptyStateMessageContents;

        // TODO: load these from settings
        SortDirectionCurrent = itemProviderFactory.SortDirectionDefault;
        IsAscending = SortDirectionCurrent == ListSortDirection.Ascending;

        IsWinnerTop = SortDirectionCurrent == ListSortDirection.Ascending &&
                      itemProviderFactory.IndexOverrideBehavior == IndexOverrideBehavior.SmallerIndexWins;

        var sortDirectionObservable = this.WhenAnyValue(vm => vm.SortDirectionCurrent)
            .Replay(1);

        var adapter = new LoadOrderTreeDataGridAdapter(provider, sortDirectionObservable, serviceProvider);
        Adapter = adapter;
        Adapter.ViewHierarchical.Value = true;

        // We have different alerts based on the type of load order, so we key in the SortOrderTypeId
        AlertSettingsWrapper = new AlertSettingsWrapper(settingsManager, $"LoadOrder Alert Type:{itemProviderFactory.SortOrderTypeId}");
        
        ToggleAlertCommand = ReactiveCommand.Create(() => { AlertSettingsWrapper.ToggleAlert(); });
        
        LearnMoreAlertCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await osInterop.OpenUrl(new Uri(itemProviderFactory.LearnMoreUrl));
        });

        SwitchSortDirectionCommand = ReactiveCommand.Create(() =>
            {
                SortDirectionCurrent = IsAscending
                    ? ListSortDirection.Descending 
                    : ListSortDirection.Ascending;
            }
        );

        this.WhenActivated(d =>
            {
                Adapter.Activate().DisposeWith(d);

                sortDirectionObservable.Connect()
                    .DisposeWith(d);

                // Update IsWinnerTop
                sortDirectionObservable.Subscribe(sortDirection =>
                    {
                        IsAscending = sortDirection == ListSortDirection.Ascending;
                        IsWinnerTop = IsAscending &&
                                      itemProviderFactory.IndexOverrideBehavior == IndexOverrideBehavior.SmallerIndexWins;
                    })
                    .DisposeWith(d);

                // Move up/down commands
                adapter.MessageSubject
                    .SubscribeAwait(async (payload, cancellationToken) =>
                        {
                            var (item, delta) = payload.Match(
                                moveUpPayload =>
                                {
                                    var deltaUp = SortDirectionCurrent == ListSortDirection.Ascending ? -1 : +1;
                                    return (provider.GetSortableItem(moveUpPayload.Item.Key), deltaUp);
                                },
                                moveDownPayload =>
                                {
                                    var deltaDown = SortDirectionCurrent == ListSortDirection.Ascending ? +1 : -1;
                                    return (provider.GetSortableItem(moveDownPayload.Item.Key), deltaDown);
                                }
                            );
                            
                            if (!item.HasValue) return;
                            await provider.SetRelativePosition(item.Value, delta, cancellationToken);
                        })
                    .DisposeWith(d);

                // Drag and drop
                adapter.RowDragOverSubject
                    .Subscribe(dragDropPayload =>
                        {
                            var (sourceModels, targetModel, eventArgs) = dragDropPayload;

                            if (eventArgs.Position != TreeDataGridRowDropPosition.Inside) return;
                            
                            // Update the drop position for the inside case to be before or after
                            eventArgs.Position = PointerIsInVerticalTopHalf(eventArgs) ? TreeDataGridRowDropPosition.Before : TreeDataGridRowDropPosition.After;
                        }
                    );
                
                adapter.RowDropSubject
                    .SubscribeAwait(async (dragDropPayload, cancellationToken) =>
                        {
                            var (sourceModels, targetModel, eventArgs) = dragDropPayload;
                            
                            // Determine source items
                            var sourceItems = sourceModels
                                .Select(model => provider.GetSortableItem(model.Key))
                                .Where(optionalItem => optionalItem.HasValue)
                                .Select(optionalItem => optionalItem.Value)
                                .ToArray();
                            if (sourceItems.Length == 0) return;
                            
                            // Determine target item
                            var targetItem = provider.GetSortableItem(targetModel.Key);
                            if (!targetItem.HasValue) return;
                            
                            // Determine relative position
                            TargetRelativePosition relativePosition;
                            switch (eventArgs.Position)
                            {
                                case TreeDataGridRowDropPosition.Before when SortDirectionCurrent == ListSortDirection.Ascending:
                                case TreeDataGridRowDropPosition.After when SortDirectionCurrent == ListSortDirection.Descending:
                                    relativePosition = TargetRelativePosition.BeforeTarget;
                                    break;
                                case TreeDataGridRowDropPosition.After when SortDirectionCurrent == ListSortDirection.Ascending:
                                case TreeDataGridRowDropPosition.Before when SortDirectionCurrent == ListSortDirection.Descending:
                                    relativePosition = TargetRelativePosition.AfterTarget;
                                    break;
                                case TreeDataGridRowDropPosition.Inside when SortDirectionCurrent == ListSortDirection.Ascending:
                                    relativePosition = PointerIsInVerticalTopHalf(eventArgs) ? TargetRelativePosition.BeforeTarget : TargetRelativePosition.AfterTarget;
                                    break;
                                case TreeDataGridRowDropPosition.Inside when SortDirectionCurrent == ListSortDirection.Descending:
                                    relativePosition = PointerIsInVerticalTopHalf(eventArgs) ? TargetRelativePosition.AfterTarget : TargetRelativePosition.BeforeTarget;
                                    break;
                                case TreeDataGridRowDropPosition.None:
                                    // Invalid target, no move
                                    return;
                                default:
                                    return;
                            }
                            
                            await provider.MoveItemsTo(sourceItems, targetItem.Value, relativePosition, cancellationToken);
                        },
                        awaitOperation: AwaitOperation.Drop)
                    .DisposeWith(d);
            }
        );
    }
    
    private static bool PointerIsInVerticalTopHalf(TreeDataGridRowDragEventArgs eventArgs)
    {
        var positionY = eventArgs.Inner.GetPosition(eventArgs.TargetRow).Y / eventArgs.TargetRow.Bounds.Height;
        return positionY < 0.5;
    }
}

public readonly record struct MoveUpCommandPayload(CompositeItemModel<Guid> Item);
public readonly record struct MoveDownCommandPayload(CompositeItemModel<Guid> Item);


public class LoadOrderTreeDataGridAdapter : TreeDataGridAdapter<CompositeItemModel<Guid>, Guid>,
    ITreeDataGirdMessageAdapter<OneOf<MoveUpCommandPayload, MoveDownCommandPayload>>
{
    private readonly ILoadoutSortableItemProvider _sortableItemsProvider;
    private readonly ILoadOrderDataProvider[] _loadOrderDataProviders;
    private readonly R3.Observable<ListSortDirection> _sortDirectionObservable;
    private readonly IObservable<ISortedChangeSet<CompositeItemModel<Guid>, Guid>> _sortedItems;
    private readonly System.Reactive.Subjects.Subject<IComparer<CompositeItemModel<Guid>>> _resortSubject = new(); 
    private readonly CompositeDisposable _disposables = new();

    public Subject<OneOf<MoveUpCommandPayload, MoveDownCommandPayload>> MessageSubject { get; } = new();
    
    public LoadOrderTreeDataGridAdapter(
        ILoadoutSortableItemProvider sortableItemsProvider,
        IObservable<ListSortDirection> sortDirectionObservable,
        IServiceProvider serviceProvider)
    {
        _sortableItemsProvider = sortableItemsProvider;
        _sortDirectionObservable = sortDirectionObservable.ToObservable();

        _loadOrderDataProviders = serviceProvider.GetServices<ILoadOrderDataProvider>().ToArray();
        
        var itemsChangeSet = _loadOrderDataProviders
            .Select(x => x.ObserveLoadOrder(_sortableItemsProvider, _sortDirectionObservable)).MergeChangeSets();
        
        var ascendingComparer = SortExpressionComparer<CompositeItemModel<Guid>>.Ascending(
            item => item.Get<LoadOrderComponents.IndexComponent>(LoadOrderColumns.IndexColumn.IndexComponentKey).SortIndex.Value
        );
        var descendingComparer = SortExpressionComparer<CompositeItemModel<Guid>>.Descending(
            item => item.Get<LoadOrderComponents.IndexComponent>(LoadOrderColumns.IndexColumn.IndexComponentKey).SortIndex.Value
        );
        
        var comparerObservable = sortDirectionObservable.Select(sortDirection =>
            {
                return sortDirection == ListSortDirection.Ascending
                    ? ascendingComparer
                    : descendingComparer;
            }
        );
        
        // NOTE(Al12rs): Sorting is a bit of a nightmare with the Adapter at the moment.
        // Cysharp ObservableCollections no longer have a SortedView to apply synchronized sorting to a collection.
        // The ApplyChanges method used to populate the ObservableList from the changeSet puts new items in based on the
        // order of the changes received, rather than using accurate indices (it doesn't take a ISortedChangeSet).
        //
        // By sorting as the last possible step, the passed changeset retains some sorting indices, which makes the sorting mostly accurate.
        // This doesn't update correctly though when the sorting direction is changed.
        // To handle that, we manually trigger a sorting of the Roots list when the sorting direction changes.
        // Yeah, it's pretty ugly.
        _sortedItems = itemsChangeSet.Sort(comparerObservable);

        var activationDisposable = this.WhenActivated( (self, disposables)  =>
            {
                // Sort the Roots list when the sorting direction changes, as it doesn't update correctly otherwise
                comparerObservable
                    .Subscribe(comparer => _resortSubject.OnNext(comparer))
                    .AddTo(disposables);
                
                _resortSubject.Subscribe(comparer => Roots.Sort(comparer))
                    .AddTo(disposables);
            }
        );
        activationDisposable.DisposeWith(_disposables);
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<Guid> model)
    {
        base.BeforeModelActivationHook(model);
        
        // Move up command
        model.SubscribeToComponentAndTrack<LoadOrderComponents.IndexComponent, LoadOrderTreeDataGridAdapter>(
            key: LoadOrderColumns.IndexColumn.IndexComponentKey,
            state: this,
            factory: static (adapter, itemModel, component) => component.MoveUp
                .Subscribe((adapter, itemModel, component),
                    static (_, tuple) =>
                    {
                        var (adapter, itemModel, _) = tuple;
                        adapter.MessageSubject.OnNext(new MoveUpCommandPayload(itemModel));
                    }
                )
        );
        
        // Move down command
        model.SubscribeToComponentAndTrack<LoadOrderComponents.IndexComponent, LoadOrderTreeDataGridAdapter>(
            key: LoadOrderColumns.IndexColumn.IndexComponentKey,
            state: this,
            factory: static (adapter, itemModel, component) => component.MoveDown
                .Subscribe((adapter, itemModel, component),
                    static (_, tuple) =>
                    {
                        var (adapter, itemModel, _) = tuple;
                        adapter.MessageSubject.OnNext(new MoveDownCommandPayload(itemModel));
                    }
                )
        );
        
        // IsActive styling
        model.SubscribeToComponentAndTrack<ValueComponent<bool>, LoadOrderTreeDataGridAdapter>(
            key: LoadOrderColumns.IsActiveComponentKey,
            state: this,
            factory: static (adapter, itemModel, component) => component.Value
                .Subscribe((adapter, itemModel, component),
                    static (_, tuple) =>
                    {
                        var (_, itemModel, component) = tuple;
                        itemModel.SetStyleFlag(LoadOrderColumns.IsActiveStyleTag, component.Value.Value);
                    }
                )
        );
    }
    

    protected override IObservable<IChangeSet<CompositeItemModel<Guid>, Guid>> GetRootsObservable(bool viewHierarchical)
    {
        return _sortedItems;
    }

    protected override IColumn<CompositeItemModel<Guid>>[] CreateColumns(bool viewHierarchical)
    {
        var indexColumn = ColumnCreator.Create<Guid, LoadOrderColumns.IndexColumn>(
            columnHeader: _sortableItemsProvider.ParentFactory.IndexColumnHeader,
            canUserSortColumn: false,
            canUserResizeColumn: false
        );
        
        var expanderColumn = ITreeDataGridItemModel<CompositeItemModel<Guid>, Guid>.CreateExpanderColumn(indexColumn);

        return
        [
            expanderColumn,
            ColumnCreator.Create<Guid, LoadOrderColumns.DisplayNameColumn>(
                columnHeader: _sortableItemsProvider.ParentFactory.DisplayNameColumnHeader,
                canUserSortColumn: false,
                canUserResizeColumn: false
            ),
            ColumnCreator.Create<Guid, LoadOrderColumns.ModNameColumn>(
                canUserSortColumn: false,
                canUserResizeColumn: false
            )
        ];
    }

    private bool _isDisposed;

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                R3.Disposable.Dispose(_disposables, MessageSubject);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
