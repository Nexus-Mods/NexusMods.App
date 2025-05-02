using System.Reactive.Disposables;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.View;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialDisposable = System.Reactive.Disposables.SerialDisposable;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel;

[UsedImplicitly]
public class ViewLoadoutGroupFilesViewModel : APageViewModel<IViewLoadoutGroupFilesViewModel>, IViewLoadoutGroupFilesViewModel
{
    // IViewLoadoutGroupFilesViewModel
    [Reactive] public ViewLoadoutGroupFilesPageContext? Context { get; set; }
    [Reactive] public ReactiveCommand<NavigationInformation> OpenEditorCommand { get; [UsedImplicitly] set; }
    [Reactive] public ReactiveCommand<Unit> RemoveCommand { get; [UsedImplicitly] set; }
    [Reactive] public CompositeItemModel<EntityId>? SelectedItem { get; set; }
    [Reactive] public ViewLoadoutGroupFilesTreeDataGridAdapter? FileTreeAdapter { get; set; }
    
    // Private state.
    private DisposableBag _disposables = new();
    private readonly SerialDisposable _selectedItemsSubscription = new();
    
    public ViewLoadoutGroupFilesViewModel(
        ILogger<ViewLoadoutGroupFilesViewModel> logger,
        IWindowManager windowManager,
        IServiceProvider provider,
        IConnection connection) : base(windowManager)
    {
        TabIcon = IconValues.FolderOpen;
        TabTitle = "File Tree";

        OpenEditorCommand = this.ObservePropertyChanged(vm => vm.SelectedItem)
            .Select(connection, static (item, connection) =>
                {
                    // Directories in future code will not constitute valid entities,
                    // so we filter out here. 
                    if (item == null)
                        return false;

                    var loadoutFile = new LoadoutFile.ReadOnly(connection.Db, item.Key);
                    return loadoutFile.IsValid();
                }
            )
            .ToReactiveCommand<NavigationInformation>(info =>
            {
                // Note(sewer): Is it possible to avoid a double entity load here?
                var fileId = SelectedItem!.Key;
                var loadoutFile = new LoadoutFile.ReadOnly(connection.Db, fileId);
                if (!loadoutFile.IsValid())
                {
                    logger.LogError("Unable to find Loadout File with ID `{FileId}`. This is indicative of a bug.", fileId);
                    return;
                }

                var pageData = new PageData
                {
                    FactoryId = TextEditorPageFactory.StaticId,
                    Context = new TextEditorPageContext
                    {
                        FileId = loadoutFile.LoadoutFileId,
                        FilePath = loadoutFile.AsLoadoutItemWithTargetPath().TargetPath.Item3,
                        IsReadOnly = Context!.IsReadOnly,
                    },
                };

                var workspaceController = windowManager.ActiveWorkspaceController;

                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                var workspaceId = workspaceController.ActiveWorkspaceId;
                workspaceController.OpenPage(workspaceId, pageData, behavior);
            })
            .AddTo(ref _disposables);

        var writeableContextObservable = this.ObservePropertyChanged(vm => vm.Context).Select(context => context is { IsReadOnly: false });
        var hasSelectionObservable = this.ObservePropertyChanged(vm => vm.SelectedItem).Select(item => item is not null);
        
        // ReSharper disable once InvokeAsExtensionMethod
        RemoveCommand = Observable.CombineLatest(
                writeableContextObservable, hasSelectionObservable,
                resultSelector: (isWritable, hasSelection) => isWritable && hasSelection
            )
            .ToReactiveCommand<Unit>(async (_, _) =>
                {
                    var fileId = SelectedItem!.Key;
                    var loadoutFile = new LoadoutFile.ReadOnly(connection.Db, fileId);
                    if (!loadoutFile.IsValid())
                    {
                        logger.LogError("Unable to find Loadout File with ID `{FileId}`. This is indicative of a bug.", fileId);
                        return;
                    }

                    // TODO: Support directories here, once that's integrated.
                    // The RemoveFileOrFolder API already handles this, when we integrate folders,
                    // which is why we use 'GamePath' as entry point.
                    var gamePath = loadoutFile.AsLoadoutItemWithTargetPath().TargetPath;
                    var result = await LoadoutItemGroupHelpers.RemoveFileOrFolder(connection, Context!.GroupIds, gamePath, requireAllGroups: false);
                    if (result == LoadoutItemGroupHelpers.GroupOperationStatus.NoItemsDeleted)
                        logger.LogError("Unable to find Loadout files with path `{Path}` in groups: {Groups}", 
                            gamePath, string.Join(", ", Context!.GroupIds));
                }
            )
            .AddTo(ref _disposables);

        _selectedItemsSubscription.AddTo(ref _disposables);
        
        this.WhenActivated(disposables =>
        {
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
                        
                        // Update the selection subscription
                        // Note: This auto disposes last.
                        _selectedItemsSubscription.Disposable = FileTreeAdapter.SelectedModels
                            .ObserveCountChanged(notifyCurrentCount: true)
                            .Subscribe(FileTreeAdapter, (_, adapter) => {
                                SelectedItem = adapter.SelectedModels.FirstOrDefault();
                            });
            
                        // Initial update
                        SelectedItem = FileTreeAdapter.SelectedModels.FirstOrDefault();
                    }
                )
                .Subscribe()
                .DisposeWith(disposables);
        });
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
