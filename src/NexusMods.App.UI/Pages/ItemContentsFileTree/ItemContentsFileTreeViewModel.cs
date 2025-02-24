using System.Reactive.Disposables;
using Avalonia.Controls.Selection;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Extensions.BCL;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using R3;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree;

[UsedImplicitly]
public class ItemContentsFileTreeViewModel : APageViewModel<IItemContentsFileTreeViewModel>, IItemContentsFileTreeViewModel
{
    [Reactive] public ItemContentsFileTreePageContext? Context { get; set; }
    [Reactive] public IFileTreeViewModel? FileTreeViewModel { get; [UsedImplicitly] private set; }
    [Reactive] public FileTreeNodeViewModel? SelectedItem { get; [UsedImplicitly] private set; }

    public ReactiveCommand<NavigationInformation> OpenEditorCommand { get; }
    public ReactiveCommand<Unit> RemoveCommand { get; }
    
    private DisposableBag _disposables;

    public ItemContentsFileTreeViewModel(
        ILogger<ItemContentsFileTreeViewModel> logger,
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
                var group = LoadoutItemGroup.Load(connection.Db, Context!.GroupId);
                var found = group
                    .Children
                    .OfTypeLoadoutItemWithTargetPath()
                    .OfTypeLoadoutFile()
                    .TryGetFirst(x => x.AsLoadoutItemWithTargetPath().TargetPath == gamePath, out var loadoutFile);

                if (!found)
                {
                    logger.LogError("Unable to find Loadout File with path `{Path}` in group `{Group}`", gamePath, group.AsLoadoutItem().Name);
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

        
        // ReSharper disable once InvokeAsExtensionMethod
        RemoveCommand = Observable.CombineLatest(
                this.ObservePropertyChanged(vm => vm.Context)
                    .Select(context => context is { IsReadOnly: false }),
                this.ObservePropertyChanged(vm => vm.SelectedItem)
                    .Select(item => item is not null),
                resultSelector: (isWritable, hasSelection) => isWritable && hasSelection
            )
            .ToReactiveCommand<Unit>(async (_, _) =>
                {
                    var gamePath = SelectedItem!.Key;
                    var group = LoadoutItemGroup.Load(connection.Db, Context!.GroupId);
                    var loadoutItemsToDelete = group
                        .Children
                        .OfTypeLoadoutItemWithTargetPath()
                        .Where(item => item.TargetPath.Item2.Equals(gamePath.LocationId) && item.TargetPath.Item3.StartsWith(gamePath.Path))
                        .ToArray();

                    if (loadoutItemsToDelete.Length == 0)
                    {
                        logger.LogError("Unable to find Loadout files with path `{Path}` in group `{Group}`", gamePath, group.AsLoadoutItem().Name);
                        return;
                    }

                    using var tx = connection.BeginTransaction();

                    foreach (var loadoutItem in loadoutItemsToDelete)
                    {
                        tx.Delete(loadoutItem, recursive: false);
                    }

                    await tx.Commit();

                    // Refresh the file tree, currently by re-creating it which isn't super great
                    FileTreeViewModel = new LoadoutItemGroupFileTreeViewModel(group.Rebase());
                }
            )
            .AddTo(ref _disposables);

        this.WhenActivated(disposables =>
        {
            // Populate the file tree
            this.ObservePropertyChanged(vm => vm.Context)
                .WhereNotNull()
                .Select(context => LoadoutItemGroup.Load(connection.Db, context.GroupId))
                .Where(group => group.IsValid())
                .Do(group => TabTitle = group.AsLoadoutItem().Name)
                .Select(group => new LoadoutItemGroupFileTreeViewModel(group))
                .Do(viewModel => FileTreeViewModel = viewModel)
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
        });
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
