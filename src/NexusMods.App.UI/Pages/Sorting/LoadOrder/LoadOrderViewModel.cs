using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.Settings;
using NexusMods.App.UI.Controls;
using R3;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.Sdk;
using NexusMods.UI.Sdk;
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

    public TreeDataGridAdapter<CompositeItemModel<ISortItemKey>, ISortItemKey> Adapter { get; }

    [UsedImplicitly]
    public LoadOrderViewModel(
        IServiceProvider serviceProvider,
        ISortOrderVariety sortOrderVariety,
        LoadoutId loadoutId)
    {
        var osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();

        var optionalSortOrderId = sortOrderVariety.GetSortOrderIdFor(loadoutId);
        if (!optionalSortOrderId.HasValue)
            throw new InvalidOperationException($"No sort order found for loadout {loadoutId} and variety {sortOrderVariety.SortOrderVarietyId}");
        var sortOrderId = optionalSortOrderId.Value;
        
        SortOrderName = sortOrderVariety.SortOrderUiMetadata.SortOrderName;
        SortOrderName = sortOrderVariety.SortOrderUiMetadata.SortOrderName;
        InfoAlertTitle = sortOrderVariety.SortOrderUiMetadata.OverrideInfoTitle;
        InfoAlertBody = sortOrderVariety.SortOrderUiMetadata.OverrideInfoMessage;
        TrophyToolTip = sortOrderVariety.SortOrderUiMetadata.WinnerIndexToolTip;
        EmptyStateMessageTitle = sortOrderVariety.SortOrderUiMetadata.EmptyStateMessageTitle;
        EmptyStateMessageContents = sortOrderVariety.SortOrderUiMetadata.EmptyStateMessageContents;

        // TODO: load these from settings
        SortDirectionCurrent = sortOrderVariety.SortDirectionDefault;
        IsAscending = SortDirectionCurrent == ListSortDirection.Ascending;

        IsWinnerTop = SortDirectionCurrent == ListSortDirection.Ascending &&
                      sortOrderVariety.IndexOverrideBehavior == IndexOverrideBehavior.SmallerIndexWins;

        var sortDirectionObservable = this.WhenAnyValue(vm => vm.SortDirectionCurrent)
            .Replay(1);

        var adapter = new LoadOrderTreeDataGridAdapter(sortOrderVariety, loadoutId, sortDirectionObservable, serviceProvider);
        Adapter = adapter;
        Adapter.ViewHierarchical.Value = true;

        // We have different alerts based on the type of load order, so we key in the SortOrderTypeId
        AlertSettingsWrapper = new AlertSettingsWrapper(settingsManager, $"LoadOrder Alert Type:{sortOrderVariety.SortOrderVarietyId}");
        
        ToggleAlertCommand = ReactiveCommand.Create(() => { AlertSettingsWrapper.ToggleAlert(); });
        
        LearnMoreAlertCommand = ReactiveCommand.Create(() => osInterop.OpenUri(new Uri(sortOrderVariety.SortOrderUiMetadata.LearnMoreUrl)));

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
                                      sortOrderVariety.IndexOverrideBehavior == IndexOverrideBehavior.SmallerIndexWins;
                    })
                    .DisposeWith(d);

                // Move up/down commands
                adapter.MessageSubject
                    .SubscribeAwait(async (payload, cancellationToken) =>
                        {
                            var (key, delta) = payload.Match(
                                moveUpPayload =>
                                {
                                    var deltaUp = SortDirectionCurrent == ListSortDirection.Ascending ? -1 : +1;
                                    return (moveUpPayload.Item.Key, deltaUp);
                                },
                                moveDownPayload =>
                                {
                                    var deltaDown = SortDirectionCurrent == ListSortDirection.Ascending ? +1 : -1;
                                    return (moveDownPayload.Item.Key, deltaDown);
                                }
                            );
                            
                            await sortOrderVariety.MoveItemDelta(sortOrderId, key, delta, token: cancellationToken);
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
                            var keysToMove = sourceModels.Select(item => item.Key).ToArray();
                            if (keysToMove.Length == 0) return;

                            // Determine target item
                            var dropTargetKey = targetModel.Key;
                            
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
                            
                            await sortOrderVariety.MoveItems(sortOrderId, keysToMove, dropTargetKey, relativePosition, token: cancellationToken);
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

public readonly record struct MoveUpCommandPayload(CompositeItemModel<ISortItemKey> Item);

public readonly record struct MoveDownCommandPayload(CompositeItemModel<ISortItemKey> Item);

public class LoadOrderTreeDataGridAdapter : TreeDataGridAdapter<CompositeItemModel<ISortItemKey>, ISortItemKey>,
    ITreeDataGirdMessageAdapter<OneOf<MoveUpCommandPayload, MoveDownCommandPayload>>
{
    private readonly ISortOrderVariety _sortOrderVariety;
    private readonly LoadoutId _loadoutId;
    private readonly ILoadOrderDataProvider[] _loadOrderDataProviders;
    private readonly R3.Observable<ListSortDirection> _sortDirectionObservable;
    private readonly System.Reactive.Subjects.Subject<IComparer<CompositeItemModel<ISortItemKey>>> _resortSubject = new(); 
    private readonly CompositeDisposable _disposables = new();

    public Subject<OneOf<MoveUpCommandPayload, MoveDownCommandPayload>> MessageSubject { get; } = new();

    public LoadOrderTreeDataGridAdapter(
        ISortOrderVariety sortOrderVariety,
        LoadoutId loadoutId,
        IObservable<ListSortDirection> sortDirectionObservable,
        IServiceProvider serviceProvider)
    {
        _loadoutId = loadoutId;
        _sortOrderVariety = sortOrderVariety;
        _sortDirectionObservable = sortDirectionObservable.ToObservable();

        _loadOrderDataProviders = serviceProvider.GetServices<ILoadOrderDataProvider>().ToArray();
        
        var ascendingComparer = SortExpressionComparer<CompositeItemModel<ISortItemKey>>.Ascending(
            item => item.Get<LoadOrderComponents.IndexComponent>(LoadOrderColumns.IndexColumn.IndexComponentKey).SortIndex.Value
        );
        var descendingComparer = SortExpressionComparer<CompositeItemModel<ISortItemKey>>.Descending(
            item => item.Get<LoadOrderComponents.IndexComponent>(LoadOrderColumns.IndexColumn.IndexComponentKey).SortIndex.Value
        );
        
        var comparerObservable = sortDirectionObservable.Select(sortDirection =>
            {
                return sortDirection == ListSortDirection.Ascending
                    ? ascendingComparer
                    : descendingComparer;
            }
        );

        var activationDisposable = this.WhenActivated( (self, disposables)  =>
            {
                // Sort the Roots list when the sorting direction changes, as it doesn't update correctly otherwise
                comparerObservable
                    .Subscribe(comparer => CustomSortComparer.Value = comparer)
                    .AddTo(disposables);
            }
        );
        activationDisposable.DisposeWith(_disposables);
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<ISortItemKey> model)
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

    protected override IObservable<IChangeSet<CompositeItemModel<ISortItemKey>, ISortItemKey>> GetRootsObservable(bool viewHierarchical)
    {
        return _loadOrderDataProviders
            .Select(x => x.ObserveLoadOrder(_sortOrderVariety, _loadoutId, _sortDirectionObservable))
            .MergeChangeSets();
    }

    protected override IColumn<CompositeItemModel<ISortItemKey>>[] CreateColumns(bool viewHierarchical)
    {
        var indexColumn = ColumnCreator.Create<ISortItemKey, LoadOrderColumns.IndexColumn>(
            columnHeader: _sortOrderVariety.SortOrderUiMetadata.IndexColumnHeader,
            canUserSortColumn: false,
            canUserResizeColumn: false
        );
        
        var expanderColumn = ITreeDataGridItemModel<CompositeItemModel<ISortItemKey>, ISortItemKey>.CreateExpanderColumn(indexColumn);

        return
        [
            expanderColumn,
            ColumnCreator.Create<ISortItemKey, LoadOrderColumns.DisplayNameColumn>(
                columnHeader: _sortOrderVariety.SortOrderUiMetadata.DisplayNameColumnHeader,
                canUserSortColumn: false,
                canUserResizeColumn: false
            ),
            ColumnCreator.Create<ISortItemKey, LoadOrderColumns.ModNameColumn>(
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
