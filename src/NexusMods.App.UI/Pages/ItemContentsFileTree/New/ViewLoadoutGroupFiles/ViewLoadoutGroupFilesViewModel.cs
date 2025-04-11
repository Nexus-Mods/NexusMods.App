using System.Reactive.Disposables;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles;

[UsedImplicitly]
public class ViewLoadoutGroupFilesViewModel : APageViewModel<IViewLoadoutGroupFilesViewModel>, IViewLoadoutGroupFilesViewModel
{
    [Reactive] public ItemContentsFileTreePageContext? Context { get; set; }
    [Reactive] public IFileTreeViewModel? FileTreeViewModel { get; [UsedImplicitly] private set; }
    [Reactive] public FileTreeNodeViewModel? SelectedItem { get; [UsedImplicitly] private set; }

    public ReactiveCommand<NavigationInformation> OpenEditorCommand { get; }
    public ReactiveCommand<Unit> RemoveCommand { get; }
    
    private DisposableBag _disposables;

    public ViewLoadoutGroupFilesViewModel(
        ILogger<ViewLoadoutGroupFilesViewModel> logger,
        IWindowManager windowManager,
        IConnection connection) : base(windowManager)
    {
        TabIcon = IconValues.FolderOpen;
        TabTitle = "File Tree";

        OpenEditorCommand = this.ObservePropertyChanged(vm => vm.SelectedItem)
            .Select(item => item is { IsFile: true, IsDeletion: false })
            .ToReactiveCommand<NavigationInformation>(info =>
            {
                var gamePath = SelectedItem!.Key;
                var loadoutFile = LoadoutItemGroupHelpers.FindMatchingFile(connection, Context!.GroupIds, gamePath, requireAllGroups: false);

                if (loadoutFile == null)
                {
                    logger.LogError("Unable to find Loadout File with path `{Path}` in groups: {Groups}", gamePath, string.Join(", ", Context!.GroupIds));
                    return;
                }

                var pageData = new PageData
                {
                    FactoryId = TextEditorPageFactory.StaticId,
                    Context = new TextEditorPageContext
                    {
                        FileId = loadoutFile.Value.LoadoutFileId,
                        FilePath = loadoutFile.Value.AsLoadoutItemWithTargetPath().TargetPath.Item3,
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
                    var gamePath = SelectedItem!.Key;
                    var result = await LoadoutItemGroupHelpers.RemoveFileOrFolder(connection, Context!.GroupIds, gamePath, requireAllGroups: false);
                    if (result == LoadoutItemGroupHelpers.GroupOperationStatus.NoItemsDeleted)
                        logger.LogError("Unable to find Loadout files with path `{Path}` in groups: {Groups}", 
                            gamePath, string.Join(", ", Context!.GroupIds));
                }
            )
            .AddTo(ref _disposables);

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
            /*
            this.ObservePropertyChanged(vm => vm.Context)
                .WhereNotNull()
                .Do(context => FileTreeViewModel = new LoadoutItemGroupFileTreeViewModel(context.GroupIds))
                .Subscribe()
                .DisposeWith(disposables);
            
            // Observe selected items
            this.WhenAnyValue(vm => vm.FileTreeViewModel!.TreeSource.Selection)
                .ToObservable()
                .WhereNotNull()
                .OfType<ITreeDataGridSelection, ITreeDataGridRowSelectionModel>()
                .Select(selectionModel => selectionModel.ObservePropertyChanged(model => model.SelectedItem))
                .Switch()
                .WhereNotNull()
                .OfType<object, FileTreeNodeViewModel>()
                .Subscribe(item => SelectedItem = item)
                .DisposeWith(disposables);
            */
        });
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
