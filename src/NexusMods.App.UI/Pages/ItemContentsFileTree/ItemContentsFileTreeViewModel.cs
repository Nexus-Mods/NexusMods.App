using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

namespace NexusMods.App.UI.Pages.ItemContentsFileTree;

[UsedImplicitly]
public class ItemContentsFileTreeViewModel : APageViewModel<IItemContentsFileTreeViewModel>, IItemContentsFileTreeViewModel
{
    [Reactive] public ItemContentsFileTreePageContext? Context { get; set; }
    [Reactive] public IFileTreeViewModel? FileTreeViewModel { get; [UsedImplicitly] private set; }
    [Reactive] public FileTreeNodeViewModel? SelectedItem { get; [UsedImplicitly] private set; }

    public ReactiveCommand<NavigationInformation, Unit> OpenEditorCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

    public ItemContentsFileTreeViewModel(
        ILogger<ItemContentsFileTreeViewModel> logger,
        IWindowManager windowManager,
        IConnection connection) : base(windowManager)
    {
        TabIcon = IconValues.FolderOpen;
        TabTitle = "File Tree";
        
        OpenEditorCommand = ReactiveCommand.Create<NavigationInformation>(info =>
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
                },
            };

            var workspaceController = windowManager.ActiveWorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            var workspaceId = workspaceController.ActiveWorkspaceId;
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        },
        this.WhenAnyValue(vm => vm.SelectedItem)
            .WhereNotNull()
            .Select(item => item is { IsFile: true, IsDeletion: false })
        );

        RemoveCommand = ReactiveCommand.CreateFromTask(async () =>
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
            },
            this.WhenAnyValue(vm => vm.SelectedItem)
                .WhereNotNull()
                .Select(_ => true)
        );

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.Context)
                .WhereNotNull()
                .Select(context => LoadoutItemGroup.Load(connection.Db, context.GroupId))
                .Where(group => group.IsValid())
                .Do(group => TabTitle = group.AsLoadoutItem().Name)
                .Select(group => new LoadoutItemGroupFileTreeViewModel(group))
                .Do(viewModel => FileTreeViewModel = viewModel)
                .Subscribe()
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.FileTreeViewModel!.TreeSource.Selection)
                .WhereNotNull()
                .OfType<ITreeDataGridRowSelectionModel>()
                .Select(selectionModel => selectionModel.WhenAnyValue(x => x.SelectedItem))
                .Switch()
                .WhereNotNull()
                .OfType<FileTreeNodeViewModel>()
                .BindToVM(this, vm => vm.SelectedItem)
                .DisposeWith(disposables);
        });
    }
}
