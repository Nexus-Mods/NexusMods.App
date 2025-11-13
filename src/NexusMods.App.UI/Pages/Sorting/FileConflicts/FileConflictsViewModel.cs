using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Windows;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using Humanizer;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;
using NexusMods.Sdk.Resources;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class FileConflictsViewModel : AViewModel<IFileConflictsViewModel>, IFileConflictsViewModel
{
    public FileConflictsTreeDataGridAdapter TreeDataGridAdapter { get; }

    private readonly BindableReactiveProperty<ListSortDirection> _sortDirectionCurrent;
    private readonly ILoadoutManager _loadoutManager;
    public IReadOnlyBindableReactiveProperty<ListSortDirection> SortDirectionCurrent => _sortDirectionCurrent;
    public IReadOnlyBindableReactiveProperty<bool> IsAscending { get; }

    public R3.ReactiveCommand SwitchSortDirectionCommand { get; }

    public FileConflictsViewModel(IServiceProvider serviceProvider, IWindowManager windowManager, LoadoutId loadoutId)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(connection.Db, loadoutId);
        Debug.Assert(loadout.IsValid());

        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        _loadoutManager = serviceProvider.GetRequiredService<ILoadoutManager>();

        _sortDirectionCurrent = new BindableReactiveProperty<ListSortDirection>(ListSortDirection.Ascending);
        IsAscending = _sortDirectionCurrent.Select(direction => direction == ListSortDirection.Ascending).ToBindableReactiveProperty();

        SwitchSortDirectionCommand = new R3.ReactiveCommand(_ =>
        {
            var newDirection = SortDirectionCurrent.Value == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            _sortDirectionCurrent.Value = newDirection;
        });
        
        TreeDataGridAdapter = new FileConflictsTreeDataGridAdapter(serviceProvider, connection, synchronizer, SortDirectionCurrent.AsObservable(), loadoutId);

        this.WhenActivated(disposables =>
        {
            TreeDataGridAdapter.Activate().AddTo(disposables);

            TreeDataGridAdapter.MessageSubject.SubscribeAwait(async (msg, cancellationToken) =>
            {
                await msg.Match<Task>(
                    viewConflictsMessage => HandleViewConflictsMessage(viewConflictsMessage, windowManager, serviceProvider),
                    moveUp => Move(moveUp.Item.Key, moveUp.Item.Get<FileConflictsComponents.NeighbourIds>(FileConflictsColumns.IndexColumn.NeighbourIdsComponentKey), _sortDirectionCurrent.Value, moveUp: true), 
                    moveDown => Move(moveDown.Item.Key, moveDown.Item.Get<FileConflictsComponents.NeighbourIds>(FileConflictsColumns.IndexColumn.NeighbourIdsComponentKey), _sortDirectionCurrent.Value, moveUp: false)
                );
            }).AddTo(disposables);

            // Update drop position indicator
            TreeDataGridAdapter.RowDragOverSubject.Subscribe(dragDropPayload =>
            {
                var (sourceModels, targetModel, eventArgs) = dragDropPayload;

                if (eventArgs.Position != TreeDataGridRowDropPosition.Inside) return;
                    
                // Update the drop position for the inside case to be before or after
                eventArgs.Position = PointerIsInVerticalTopHalf(eventArgs) ? TreeDataGridRowDropPosition.Before : TreeDataGridRowDropPosition.After;
            }).AddTo(disposables);

            // Handle row drops
            TreeDataGridAdapter.RowDropSubject.SubscribeAwait(async (eventPayload, _) =>
            {
                var (itemModels, targetModel, nextTarget, prevTarget, eventArgs) = eventPayload;

                var itemIds = itemModels.Select(x => (LoadoutItemGroupPriorityId)x.Key).ToArray();
                if (itemIds.Length == 0) return;

                var losingItemModel = (eventArgs.Position, SortDirectionCurrent.Value) switch
                {
                    (TreeDataGridRowDropPosition.Before, ListSortDirection.Ascending) => prevTarget,
                    (TreeDataGridRowDropPosition.Before, ListSortDirection.Descending) => targetModel,
                    (TreeDataGridRowDropPosition.After, ListSortDirection.Ascending) => targetModel,
                    (TreeDataGridRowDropPosition.After, ListSortDirection.Descending) => nextTarget,
                    (TreeDataGridRowDropPosition.Inside, ListSortDirection.Ascending) => PointerIsInVerticalTopHalf(eventArgs) ? prevTarget : targetModel,
                    (TreeDataGridRowDropPosition.Inside, ListSortDirection.Descending) => PointerIsInVerticalTopHalf(eventArgs) ? targetModel : nextTarget,
                };

                var loserId = losingItemModel.Convert(x => (LoadoutItemGroupPriorityId)x.Key);
                if (loserId.HasValue) await _loadoutManager.ResolveFileConflicts(itemIds, loserId.Value);
                else await _loadoutManager.LoseAllFileConflicts(itemIds);
            },
            awaitOperation: AwaitOperation.Drop).AddTo(disposables);
        });
    }

    private async Task Move(EntityId toMove, FileConflictsComponents.NeighbourIds neighbourIds, ListSortDirection sortDirection, bool moveUp)
    {
        var (winnerId, loserId) = (sortDirection, moveUp) switch
        {
            (ListSortDirection.Ascending, moveUp: false) => (toMove, neighbourIds.Next.Value),
            (ListSortDirection.Ascending, moveUp: true) => (neighbourIds.Prev.Value, toMove),
            (ListSortDirection.Descending, moveUp: false) => (neighbourIds.Prev.Value, toMove),
            (ListSortDirection.Descending, moveUp: true) => (toMove, neighbourIds.Next.Value),
        };

        await _loadoutManager.ResolveFileConflicts(winnerIds: [LoadoutItemGroupPriorityId.From(winnerId)], LoadoutItemGroupPriorityId.From(loserId));
    }

    private static async Task HandleViewConflictsMessage(
        FileConflictsTreeDataGridAdapter.ViewConflictsMessage msg, 
        IWindowManager windowManager, 
        IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<IConnection>().Db;
        var priority = LoadoutItemGroupPriority.Load(db, msg.PriorityId);

        var groups = msg.Conflicts
            .Select(tuple =>
            {
                var winnerPriority = LoadoutItemGroupPriority.Load(db, tuple.WinnerPriorityId);
                var winnerLoadoutItem = LoadoutItemWithTargetPath.Load(db, tuple.WinnerLoadoutItemId);

                var loserPriority = LoadoutItemGroupPriority.Load(db, tuple.LoserPriorityId);
                var loserLoadoutItem = LoadoutItemWithTargetPath.Load(db, tuple.LoserLoadoutItemId);

                var conflictPriority = LoadoutItemGroupPriority.Load(db, tuple.ConflictPriorityId);
                var conflictLoadoutItem = LoadoutItemWithTargetPath.Load(db, tuple.ConflictLoadoutItemId);

                return ((winnerPriority, winnerLoadoutItem), (loserPriority, loserLoadoutItem), (conflictPriority, conflictLoadoutItem));
            })
            .GroupBy(GamePath (tuple) => tuple.Item1.winnerLoadoutItem.TargetPath)
            .ToArray();

        var sb = new StringBuilder();

        foreach (var group in groups)
        {
            sb.AppendLine($"## {group.Key}");

            var tuples = group.ToArray();

            var winner = tuples[0].Item1.winnerPriority.Target;
            var losers = tuples.Select(x => x.Item2.loserPriority.Target).DistinctBy(x => x.Id).ToArray();

            if (priority.TargetId == winner.LoadoutItemGroupId) sb.AppendLine($"Winner: {winner.AsLoadoutItem().Name} (this)");
            else sb.AppendLine($"Winner: {winner.AsLoadoutItem().Name}");

            sb.AppendLine();
            sb.AppendLine("Losers:");
            sb.AppendLine();

            foreach (var loser in losers)
            {
                if (priority.TargetId == loser.LoadoutItemGroupId) sb.AppendLine($"* {loser.AsLoadoutItem().Name} (this)");
                else sb.AppendLine($"* {loser.AsLoadoutItem().Name}");
            }
        }

        var markdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = sb.ToString();

        _ = await windowManager.ShowDialog(DialogFactory.CreateStandardDialog(title: $"Conflicts for {priority.Target.AsLoadoutItem().Name}", new StandardDialogParameters
        {
            Markdown = markdownRendererViewModel,
        }, buttonDefinitions: [DialogStandardButtons.Close]), DialogWindowType.Modeless);
    }

    private static bool PointerIsInVerticalTopHalf(TreeDataGridRowDragEventArgs eventArgs)
    {
        var positionY = eventArgs.Inner.GetPosition(eventArgs.TargetRow).Y / eventArgs.TargetRow.Bounds.Height;
        return positionY < 0.5;
    }
}

public class FileConflictsTreeDataGridAdapter : TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>,
    ITreeDataGirdMessageAdapter<OneOf.OneOf<
        FileConflictsTreeDataGridAdapter.ViewConflictsMessage,
        FileConflictsTreeDataGridAdapter.MoveUpCommandPayload,
        FileConflictsTreeDataGridAdapter.MoveDownCommandPayload
    >>
{
    public record ViewConflictsMessage(LoadoutItemGroupPriorityId PriorityId, (EntityId WinnerPriorityId, EntityId WinnerLoadoutItemId, EntityId ConflictPriorityId, EntityId ConflictLoadoutItemId, EntityId LoserPriorityId, EntityId LoserLoadoutItemId)[] Conflicts);
    public readonly record struct MoveUpCommandPayload(CompositeItemModel<EntityId> Item);
    public readonly record struct MoveDownCommandPayload(CompositeItemModel<EntityId> Item);

    private readonly IConnection _connection;
    private readonly ILoadoutSynchronizer _synchronizer;
    private readonly IResourceLoader<EntityId, Bitmap> _modPageThumbnailPipeline;
    private readonly LoadoutId _loadoutId;
    private readonly IDisposable _activationDisposable;

    private readonly Observable<ListSortDirection> _sortDirectionObservable;
    public Subject<OneOf.OneOf<ViewConflictsMessage, MoveUpCommandPayload, MoveDownCommandPayload>> MessageSubject { get; } = new();

    public FileConflictsTreeDataGridAdapter(
        IServiceProvider serviceProvider, 
        IConnection connection, 
        ILoadoutSynchronizer synchronizer, 
        Observable<ListSortDirection> sortDirectionObservable,
        LoadoutId loadoutId) : base(serviceProvider)
    {
        _connection = connection;
        _synchronizer = synchronizer;
        _loadoutId = loadoutId;
        _modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
        
        var ascendingComparer = SortExpressionComparer<CompositeItemModel<EntityId>>.Ascending(
            item => item.Get<SharedComponents.IndexComponent>(FileConflictsColumns.IndexColumn.IndexComponentKey).SortIndex.Value
        );

        var descendingComparer = SortExpressionComparer<CompositeItemModel<EntityId>>.Descending(
            item => item.Get<SharedComponents.IndexComponent>(FileConflictsColumns.IndexColumn.IndexComponentKey).SortIndex.Value
        );

        _sortDirectionObservable = sortDirectionObservable;
        var comparerObservable = sortDirectionObservable
            .Select(direction => direction == ListSortDirection.Ascending ? ascendingComparer : descendingComparer)
            .DistinctUntilChanged();

        _activationDisposable = this.WhenActivated( (self, disposables)  =>
        {
            comparerObservable
                .Subscribe(comparer => CustomSortComparer.Value = comparer)
                .AddTo(disposables);
        });
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        // View conflicts command
        model.SubscribeToComponentAndTrack<FileConflictsComponents.ViewAction, FileConflictsTreeDataGridAdapter>(
            key: FileConflictsColumns.Actions.ViewComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandViewConflicts.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, itemModel, component) = state;
                var conflicts = GetPriorityConflicts(self._connection, itemModel.Key).ToArray();
                self.MessageSubject.OnNext(new ViewConflictsMessage(itemModel.Key, conflicts));
            })
        );

        // Move up command
        model.SubscribeToComponentAndTrack<SharedComponents.IndexComponent, FileConflictsTreeDataGridAdapter>(
            key: FileConflictsColumns.IndexColumn.IndexComponentKey,
            state: this,
            factory: static (adapter, itemModel, component) => component.MoveUp.Subscribe((adapter, itemModel, component), static (_, tuple) =>
            {
                var (adapter, itemModel, _) = tuple;
                adapter.MessageSubject.OnNext(new MoveUpCommandPayload(itemModel));
            })
        );

        // Move down command
        model.SubscribeToComponentAndTrack<SharedComponents.IndexComponent, FileConflictsTreeDataGridAdapter>(
            key: FileConflictsColumns.IndexColumn.IndexComponentKey,
            state: this,
            factory: static (adapter, itemModel, component) => component.MoveDown.Subscribe((adapter, itemModel, component), static (_, tuple) =>
            {
                var (adapter, itemModel, _) = tuple;
                adapter.MessageSubject.OnNext(new MoveDownCommandPayload(itemModel));
            })
        );
        
        // IsActive styling
        model.SubscribeToComponentAndTrack<ValueComponent<bool>, FileConflictsTreeDataGridAdapter>(
            key: FileConflictsColumns.IsActiveComponentKey,
            state: this,
            factory: static (adapter, itemModel, component) => component.Value
                .Subscribe((adapter, itemModel, component),
                    static (_, tuple) =>
                    {
                        var (_, itemModel, component) = tuple;
                        itemModel.SetStyleFlag(FileConflictsColumns.IsActiveStyleTag, component.Value.Value);
                    }
                )
        );
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        return ObservePriorityGroups(_connection, _loadoutId).OnUI().TransformWithInlineUpdate(CreateItemModel, UpdateItemModel);
    }

    private CompositeItemModel<EntityId> CreateItemModel((EntityId, long, EntityId, EntityId, long, long) tuple)
    {
        var db = _connection.Db;
        var (groupPriorityId, index, previousId, nextId, numWinningFiles, numLosingFiles) = tuple;

        var groupPriority = LoadoutItemGroupPriority.Load(db, groupPriorityId);
        var loadoutGroup = groupPriority.Target;

        var itemModel = new CompositeItemModel<EntityId>(groupPriorityId);

        itemModel.Add(SharedColumns.Name.NameComponentKey, new NameComponent(value: loadoutGroup.AsLoadoutItem().Name));
        ImageComponent? imageComponent = null;

        if (loadoutGroup.TryGetAsLibraryLinkedLoadoutItem(out var libraryLinkedLoadoutItem))
        {
            if (libraryLinkedLoadoutItem.LibraryItem.TryGetAsNexusModsLibraryItem(out var nexusLibraryItem))
            {
                imageComponent = ImageComponent.FromPipeline(
                    _modPageThumbnailPipeline, 
                    nexusLibraryItem.ModPageMetadataId, 
                    ImagePipelines.ModPageThumbnailFallback);
            }
        }

        imageComponent ??= new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback);
        itemModel.Add(SharedColumns.Name.ImageComponentKey, imageComponent);

        itemModel.Add(FileConflictsColumns.ConflictsColumn.NumConflictsComponentKey, new FileConflictsComponents.NumConflicts(
            numWinners: new ValueComponent<long>(value: numWinningFiles),
            numLosers: new ValueComponent<long>(value: numLosingFiles)
        ));

        if (loadoutGroup.AsLoadoutItem().Parent.TryGetAsCollectionGroup(out var collection))
        {
            itemModel.Add(LoadoutColumns.Collections.ComponentKey, new StringComponent(value: collection.AsLoadoutItemGroup().AsLoadoutItem().Name));
        }

        itemModel.Add(FileConflictsColumns.Actions.ViewComponentKey, new FileConflictsComponents.ViewAction(hasConflicts: numWinningFiles > 0 || numLosingFiles > 0));

        var neighbourIds = new FileConflictsComponents.NeighbourIds(previousId, nextId);
        itemModel.Add(FileConflictsColumns.IndexColumn.NeighbourIdsComponentKey, neighbourIds);

        var canExecuteMoveUp = _sortDirectionObservable.CombineLatest(neighbourIds.Prev, neighbourIds.Next, static (direction, previousId, nextId) => (direction, previousId, nextId) switch
        {
            (ListSortDirection.Ascending, previousId: { Value: > 0 }, _) => true,
            (ListSortDirection.Descending, _, nextId: { Value: > 0}) => true,
            _ => false,
        });

        var canExecuteMoveDown = _sortDirectionObservable.CombineLatest(neighbourIds.Prev, neighbourIds.Next, static (direction, previousId, nextId) => (direction, previousId, nextId) switch
        {
            (ListSortDirection.Ascending, _, nextId: { Value: > 0}) => true,
            (ListSortDirection.Descending, previousId: { Value: > 0 }, _) => true,
            _ => false,
        });

        itemModel.Add(FileConflictsColumns.IndexColumn.IndexComponentKey, new SharedComponents.IndexComponent(
            new ValueComponent<int>((int)index),
            new ValueComponent<string>(((int)index).Ordinalize()),
            canExecuteMoveUp: canExecuteMoveUp,
            canExecuteMoveDown: canExecuteMoveDown
        ));
        
        // NOTE(Al12rs): Mark all items as active for styling purposes, change this if we need to display inactive items
        itemModel.Add(FileConflictsColumns.IsActiveComponentKey, new ValueComponent<bool>(true));

        return itemModel;
    }

    private void UpdateItemModel(CompositeItemModel<EntityId> itemModel, (EntityId Id, long Index, EntityId Prev, EntityId Next, long NumWinningFiles, long NumLosingFiles) tuple)
    {
        var (_, index, previousId, nextId, numWinningFiles, numLosingFiles) = tuple;

        itemModel.Get<FileConflictsComponents.ViewAction>(FileConflictsColumns.Actions.ViewComponentKey).HasConflicts.Value = numWinningFiles > 0 || numLosingFiles > 0;

        var numConflicts = itemModel.Get<FileConflictsComponents.NumConflicts>(FileConflictsColumns.ConflictsColumn.NumConflictsComponentKey);
        numConflicts.NumWinners.Value.Value = numWinningFiles;
        numConflicts.NumLosers.Value.Value = numLosingFiles;

        var neighbourIds = itemModel.Get<FileConflictsComponents.NeighbourIds>(FileConflictsColumns.IndexColumn.NeighbourIdsComponentKey);
        neighbourIds.Prev.Value = previousId;
        neighbourIds.Next.Value = nextId;

        var indexComponent = itemModel.Get<SharedComponents.IndexComponent>(FileConflictsColumns.IndexColumn.IndexComponentKey);
        indexComponent.Index.Value.Value = (int)index;
        indexComponent.DisplaySortIndexComponent.Value.Value = ((int)index).Ordinalize();
    }

    private static Query<(EntityId WinnerPriorityId, EntityId WinnerLoadoutItemId, EntityId ConflictPriorityId, EntityId ConflictLoadoutItemId, EntityId LoserPriorityId, EntityId LoserLoadoutItemId)> GetPriorityConflicts(
        IConnection connection,
        EntityId priorityId)
    {
        // NOTE(erri120): limited by our current query engine to not supported nested lists and tuples so we have to unnest
        return connection.Query<(EntityId, EntityId, EntityId, EntityId, EntityId, EntityId)>(
            $"""
             SELECT
               Winner.PriorityId,
               Winner.LoadoutItemId,
               conflicts."unnest".PriorityId,
               conflicts."unnest".LoadoutItemId,
               losers."unnest".PriorityId,
               losers."unnest".LoadoutItemId
             FROM
               synchronizer.ConflictingPaths ({connection}) conflicting_path
               CROSS JOIN unnest(conflicting_path.Conflicts) conflicts
               CROSS JOIN unnest(conflicting_path.Losers) losers
             WHERE
               len(list_filter(conflicting_path.Conflicts, x -> x.PriorityId = {priorityId})) > 0;
             """
        );
    }

    private static IObservable<IChangeSet<(EntityId Id, long Index, EntityId Prev, EntityId Next, long NumWinningFiles, long NumLosingFiles), EntityId>> ObservePriorityGroups(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId Id, long, EntityId, EntityId, long, long)>(
            $"""
             SELECT Id, Index, Prev, Next, len(WinningFiles), len(LosingFiles)
             FROM
               synchronizer.PriorityGroups({connection})
             WHERE Loadout = {loadoutId};
             """
        ).Observe(tuple => tuple.Id);
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var indexColumn = ColumnCreator.Create<EntityId, FileConflictsColumns.IndexColumn>(canUserSortColumn: false, canUserResizeColumn: false);

        return
        [
            ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(indexColumn),
            ColumnCreator.Create<EntityId, SharedColumns.Name>(canUserSortColumn: false),
            ColumnCreator.Create<EntityId, FileConflictsColumns.ConflictsColumn>(canUserSortColumn: false),
            ColumnCreator.Create<EntityId, LoadoutColumns.Collections>(canUserSortColumn: false),
            ColumnCreator.Create<EntityId, FileConflictsColumns.Actions>(canUserSortColumn: false),
        ];
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                R3.Disposable.Dispose(_activationDisposable, MessageSubject);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
