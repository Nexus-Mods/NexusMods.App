using System.ComponentModel;
using System.Reactive.Disposables;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using ObservableCollections;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using SerialDisposable = System.Reactive.Disposables.SerialDisposable;

namespace NexusMods.App.UI.Pages.LoadoutGroupFilesPage;

[UsedImplicitly]
public class LoadoutGroupFilesViewModel : APageViewModel<ILoadoutGroupFilesViewModel>, ILoadoutGroupFilesViewModel
{
    // IViewLoadoutGroupFilesViewModel
    [Reactive] public LoadoutGroupFilesPageContext? Context { get; set; }
    [Reactive] public ReactiveCommand<NavigationInformation> OpenEditorCommand { get; [UsedImplicitly] set; } = null!;
    [Reactive] public ReactiveCommand<Unit> RemoveCommand { get; [UsedImplicitly] set; } = null!;
    [Reactive] public CompositeItemModel<GamePath>? SelectedItem { get; set; }
    [Reactive] public ViewLoadoutGroupFilesTreeDataGridAdapter? FileTreeAdapter { get; set; }
    
    public LoadoutGroupFilesViewModel(
        ILogger<LoadoutGroupFilesViewModel> logger,
        IWindowManager windowManager,
        IServiceProvider provider,
        IConnection connection) : base(windowManager)
    {
        TabIcon = IconValues.FolderOpen;
        TabTitle = "File Tree";

        this.WhenActivated(disposables =>
        {
            // Note(sewer):
            // For instant disposing of the TreeDataGridAdapter(s), for when the Context of the
            // ViewModel is dynamically changed. Since the adapter may hold a lot of memory (a whole file tree that beams events),
            // immediate disposal is preferable for saving CPU cycles and memory.
            SerialDisposable treeDataGridAdapterDisposable = new();
            
            OpenEditorCommand = this.ObservePropertyChanged(vm => vm.SelectedItem)
                .Select((this, connection), static (item, state) =>
                {
                    // If no item is currently selected, we obviously can't open the editor.
                    if (item == null)
                        return false;

                    var gamePath = item.Key;
                    var itemWithPath = LoadoutItemGroupHelpers.FindMatchingFile
                        (state.connection, state.Item1.Context!.GroupIds, gamePath, false);
                    
                    // Don't open the editor if no file is found (it's a directory),
                    // or if it is not a LoadoutFile (it's a DeletedFile).
                    return itemWithPath.HasValue && itemWithPath.Value.IsLoadoutFile();
                }
            )
            .ToReactiveCommand<NavigationInformation>(info =>
            {
                // Note(sewer): Is it possible to avoid a double entity load here?
                var gamePath = SelectedItem!.Key;
                var itemWithPath = LoadoutItemGroupHelpers.FindMatchingFile(connection, Context!.GroupIds, gamePath);

                if (!itemWithPath.HasValue || !itemWithPath.Value.TryGetAsLoadoutFile(out var loadoutItem)) 
                    return;

                var pageData = new PageData
                {
                    FactoryId = TextEditorPageFactory.StaticId,
                    Context = new TextEditorPageContext
                    {
                        FileId = loadoutItem.LoadoutFileId, // not null because command was executable
                        FilePath = gamePath.Path,
                        IsReadOnly = Context!.IsReadOnly,
                    },
                };

                var workspaceController = windowManager.ActiveWorkspaceController;

                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                var workspaceId = workspaceController.ActiveWorkspaceId;
                workspaceController.OpenPage(workspaceId, pageData, behavior);
            })
            .DisposeWith(disposables);

            var writeableContextObservable = this.ObservePropertyChanged(vm => vm.Context).Select(context => context is { IsReadOnly: false });
            var hasSelectionObservable = this.ObservePropertyChanged(vm => vm.SelectedItem).Select(item => item is not null);
            
            // ReSharper disable once InvokeAsExtensionMethod
            RemoveCommand = Observable.CombineLatest(
                    writeableContextObservable, hasSelectionObservable,
                    resultSelector: (isWritable, hasSelection) => isWritable && hasSelection
                )
                .ToReactiveCommand<Unit>(async (_, _) =>
                    {
                        var gamePath = SelectedItem!.Key;
                        
                        // Unselect last item.
                        SelectedItem = null;
                        
                        var result = await LoadoutItemGroupHelpers.RemoveFileOrFolder(connection, Context!.GroupIds, gamePath, requireAllGroups: false);
                        if (result == LoadoutItemGroupHelpers.GroupOperationStatus.NoItemsDeleted)
                            logger.LogError("Unable to find Loadout files with path `{Path}` in groups: {Groups}", 
                                gamePath, string.Join(", ", Context!.GroupIds));

                    }
                )
                .DisposeWith(disposables);
            
            // Set the title based on the first valid group
            this.ObservePropertyChanged(vm => vm.Context)
                .WhereNotNull()
                // ReSharper disable once HeapView.CanAvoidClosure
                .Select(context => LoadoutItemGroupHelpers.GetFirstValidGroup(connection, context.GroupIds))
                .Where(group => group != null)
                .Do(group => TabTitle = group!.Value.AsLoadoutItem().Name)
                .Subscribe()
                .DisposeWith(disposables);

            // Populate the file tree
            this.ObservePropertyChanged(vm => vm.Context)
                .WhereNotNull()
                .Do(context =>
                    {
                        // Dispose any existing adapter first, in case context is changed.
                        FileTreeAdapter?.Dispose();
                        FileTreeAdapter = new ViewLoadoutGroupFilesTreeDataGridAdapter(provider, new ModFilesFilter(context.GroupIds.ToArray()));

                        var compositeDisposable = new CompositeDisposable();
                        FileTreeAdapter.Activate().AddTo(compositeDisposable);
                        FileTreeAdapter.SelectedModels
                            .ObserveChanged()
                            .Subscribe(FileTreeAdapter, (_, adapter) =>
                                {
                                    SelectedItem = adapter.SelectedModels.FirstOrDefault();
                                }
                            )
                            .AddTo(compositeDisposable);
                        FileTreeAdapter.AddTo(compositeDisposable); // cleanup self.
                        
                        // Update the selection subscription
                        // Note: This auto disposes last.
                        treeDataGridAdapterDisposable.Disposable = compositeDisposable;
            
                        // Initial update
                        SelectedItem = FileTreeAdapter.SelectedModels.FirstOrDefault();
                    }
                )
                .Subscribe()
                .DisposeWith(disposables);

            treeDataGridAdapterDisposable.DisposeWith(disposables);
        });
    }
    
    /// <summary>
    /// Returns the appropriate LoadoutItemGroup of files if the selection contains a LoadoutItemGroup containing files,
    /// if the selection contains multiple LoadoutItemGroups of files, returns None.
    /// If the group is completely empty, then it is assumed to be an empty mod and it is returned.
    /// </summary>
    internal static Optional<LoadoutItemGroup.ReadOnly> GetViewModFilesLoadoutItemGroup(
        IReadOnlyCollection<LoadoutItemId> loadoutItemIds, 
        IConnection connection)
    {
        var db = connection.Db;
        // Only allow when selecting a single item, or an item with a single child
        if (loadoutItemIds.Count != 1) return Optional<LoadoutItemGroup.ReadOnly>.None;
        var currentGroupId = loadoutItemIds.First();
        
        var groupDatoms = db.Datoms(LoadoutItemGroup.Group, Null.Instance);

        while (true)
        {
            var childDatoms = db.Datoms(LoadoutItem.ParentId, currentGroupId);
            
            // If no children, assume it's an empty mod and return the group
            if (childDatoms.Count == 0) return LoadoutItemGroup.Load(db, currentGroupId);

            var childGroups = groupDatoms.MergeByEntityId(childDatoms);

            // We have no child groups, check if children are files
            if (childGroups.Count == 0)
            {
                return LoadoutItemWithTargetPath.TryGet(db, childDatoms[0].E, out _) 
                    ? LoadoutItemGroup.Load(db, currentGroupId)
                    : Optional<LoadoutItemGroup.ReadOnly>.None;
            }
            
            // Single child group, check if that group is valid
            if (childGroups.Count == 1)
            {
                currentGroupId = childGroups.First();
                continue;
            }
        
            // We have multiple child groups, return None
            if (childGroups.Count > 1) return Optional<LoadoutItemGroup.ReadOnly>.None;
        }
    }
}

/// <summary>
/// Implementation of <see cref="TreeDataGridAdapter{TModel,TKey}"/> 
/// </summary>
public class ViewLoadoutGroupFilesTreeDataGridAdapter(IServiceProvider serviceProvider, ModFilesFilter filesFilter) : TreeDataGridAdapter<CompositeItemModel<GamePath>, GamePath>(serviceProvider)
{
    private readonly LoadoutGroupFilesProvider _loadoutGroupFilesProvider = new(serviceProvider);
    
    protected override IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> GetRootsObservable(bool viewHierarchical) => _loadoutGroupFilesProvider.ObserveModFiles(filesFilter, useFullFilePaths: !viewHierarchical);

    protected override IColumn<CompositeItemModel<GamePath>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<GamePath, SharedColumns.NameWithFileIcon>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<GamePath>, GamePath>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<GamePath, SharedColumns.ItemSizeOverGamePath>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<GamePath, SharedColumns.FileCount>(),
        ];
    }
}
