using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Windows;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Resources;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class FileConflictsViewModel : AViewModel<IFileConflictsViewModel>, IFileConflictsViewModel
{
    private BindableReactiveProperty<ListSortDirection> _sortDirectionCurrent { get; set; }

    public FileConflictsTreeDataGridAdapter TreeDataGridAdapter { get; }

    public IReadOnlyBindableReactiveProperty<ListSortDirection> SortDirectionCurrent => _sortDirectionCurrent;
    
    public R3.ReactiveCommand SwitchSortDirectionCommand { get; }

    public FileConflictsViewModel(IServiceProvider serviceProvider, IWindowManager windowManager, LoadoutId loadoutId)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(connection.Db, loadoutId);
        Debug.Assert(loadout.IsValid());

        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;

        _sortDirectionCurrent = new BindableReactiveProperty<ListSortDirection>(ListSortDirection.Ascending);
        
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
                    moveUp => Task.CompletedTask, // TODO: implement move up
                    moveDown => Task.CompletedTask // TODO: implement move down
                );
            }).AddTo(disposables);
            
            // Update drop position indicator
            TreeDataGridAdapter.RowDragOverSubject
                .Subscribe(dragDropPayload =>
                    {
                        var (sourceModels, targetModel, eventArgs) = dragDropPayload;

                        if (eventArgs.Position != TreeDataGridRowDropPosition.Inside) return;
                            
                        // Update the drop position for the inside case to be before or after
                        eventArgs.Position = PointerIsInVerticalTopHalf(eventArgs) ? 
                            TreeDataGridRowDropPosition.Before : TreeDataGridRowDropPosition.After;
                        
                        // TODO: set eventArgs.Position = TreeDataGridRowDropPosition.None to disallow drop in invalid cases
                    }
                )
                .AddTo(disposables);
            
            // Handle row drops
            TreeDataGridAdapter.RowDropSubject
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
                                case TreeDataGridRowDropPosition.Before when SortDirectionCurrent.Value == ListSortDirection.Ascending:
                                case TreeDataGridRowDropPosition.After when SortDirectionCurrent.Value == ListSortDirection.Descending:
                                    relativePosition = TargetRelativePosition.BeforeTarget;
                                    break;
                                case TreeDataGridRowDropPosition.After when SortDirectionCurrent.Value == ListSortDirection.Ascending:
                                case TreeDataGridRowDropPosition.Before when SortDirectionCurrent.Value == ListSortDirection.Descending:
                                    relativePosition = TargetRelativePosition.AfterTarget;
                                    break;
                                case TreeDataGridRowDropPosition.Inside when SortDirectionCurrent.Value == ListSortDirection.Ascending:
                                    relativePosition = PointerIsInVerticalTopHalf(eventArgs) ? TargetRelativePosition.BeforeTarget : TargetRelativePosition.AfterTarget;
                                    break;
                                case TreeDataGridRowDropPosition.Inside when SortDirectionCurrent.Value == ListSortDirection.Descending:
                                    relativePosition = PointerIsInVerticalTopHalf(eventArgs) ? TargetRelativePosition.AfterTarget : TargetRelativePosition.BeforeTarget;
                                    break;
                                case TreeDataGridRowDropPosition.None:
                                    // Invalid target, no move
                                    return;
                                default:
                                    return;
                            }
                            
                            // TODO: perform the move operation
                        },
                        awaitOperation: AwaitOperation.Drop)
                    .AddTo(disposables);
            
        });
    }
    
    private static async Task HandleViewConflictsMessage(
        FileConflictsTreeDataGridAdapter.ViewConflictsMessage msg, 
        IWindowManager windowManager, 
        IServiceProvider serviceProvider)
    {
        var markdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownRendererViewModel.Contents = msg.Markdown;

        _ = await windowManager.ShowDialog(
            DialogFactory.CreateStandardDialog(title: $"Conflicts for {msg.Group.AsLoadoutItem().Name}", new StandardDialogParameters
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
    public record ViewConflictsMessage(LoadoutItemGroup.ReadOnly Group, string Markdown);
    public readonly record struct MoveUpCommandPayload(CompositeItemModel<EntityId> Item);
    public readonly record struct MoveDownCommandPayload(CompositeItemModel<EntityId> Item);
    
    private readonly IConnection _connection;
    private readonly ILoadoutSynchronizer _synchronizer;
    private readonly IResourceLoader<EntityId, Bitmap> _modPageThumbnailPipeline;
    private readonly LoadoutId _loadoutId;
    private readonly CompositeDisposable _disposables = new();

    public Subject<OneOf.OneOf<ViewConflictsMessage, MoveUpCommandPayload, MoveDownCommandPayload>> MessageSubject { get; } = new();

    public FileConflictsTreeDataGridAdapter(
        IServiceProvider serviceProvider, 
        IConnection connection, 
        ILoadoutSynchronizer synchronizer, 
        Observable<ListSortDirection> sortDirectionObservable,
        LoadoutId loadoutId)
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
        
        var comparerObservable = sortDirectionObservable
            .Select(direction => direction == ListSortDirection.Ascending ? ascendingComparer : descendingComparer)
            .DistinctUntilChanged();
        
        this.WhenActivated( (self, disposables)  =>
            {
                comparerObservable
                    .Subscribe(comparer => CustomSortComparer.Value = comparer)
                    .AddTo(disposables);
            }
        );
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        // View conflicts command
        model.SubscribeToComponentAndTrack<FileConflictsComponents.ViewAction, FileConflictsTreeDataGridAdapter>(
            key: FileConflictsColumns.Actions.ViewComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandViewConflicts.Subscribe((self, itemModel, component), 
                    static (_, state) =>
            {
                var (self, _, component) = state;
                var markdown = component.CreateMarkdown();

                self.MessageSubject.OnNext(new ViewConflictsMessage(component.Group, markdown));
            })
        );
        
        // Move up command
        model.SubscribeToComponentAndTrack<SharedComponents.IndexComponent, FileConflictsTreeDataGridAdapter>(
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
        model.SubscribeToComponentAndTrack<SharedComponents.IndexComponent, FileConflictsTreeDataGridAdapter>(
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
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        var sourceCache = new SourceCache<CompositeItemModel<EntityId>, EntityId>(x => x.Key);

        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var conflictsByGroup = _synchronizer.GetFileConflictsByParentGroup(loadout);
        var conflictsByPath = _synchronizer.GetFileConflicts(loadout).ToFrozenDictionary();

        var enumerable = conflictsByGroup.Select(kv => ToItemModel(kv, conflictsByPath));
        sourceCache.AddOrUpdate(enumerable);

        return sourceCache.Connect();
    }

    private CompositeItemModel<EntityId> ToItemModel(
        KeyValuePair<LoadoutItemGroup.ReadOnly, LoadoutFile.ReadOnly[]> kv, 
        FrozenDictionary<GamePath, FileConflictGroup> conflictsByPath)
    {
        var (loadoutGroup, loadoutFiles) = kv;
        var itemModel = new CompositeItemModel<EntityId>(loadoutGroup.Id);

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

        itemModel.Add(FileConflictsColumns.Actions.ViewComponentKey, 
            new FileConflictsComponents.ViewAction(loadoutGroup, loadoutFiles, conflictsByPath));
        
        // TODO: populate with real data
        itemModel.Add(FileConflictsColumns.IndexColumn.IndexComponentKey, new SharedComponents.IndexComponent(
            new ValueComponent<int>(0), new ValueComponent<string>("0"),
            Observable.Return(false), Observable.Return(false))
        );

        return itemModel;
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var indexColumn = ColumnCreator.Create<EntityId, FileConflictsColumns.IndexColumn>(
            canUserSortColumn: false, canUserResizeColumn: false);
        
        return
        [
            ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(indexColumn),
            ColumnCreator.Create<EntityId, SharedColumns.Name>(canUserSortColumn: false),
            ColumnCreator.Create<EntityId, FileConflictsColumns.Actions>(canUserSortColumn: false),
        ];
    }
}
