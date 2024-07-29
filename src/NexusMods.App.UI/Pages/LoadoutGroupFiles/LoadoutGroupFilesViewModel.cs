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
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGroupFiles;

[UsedImplicitly]
public class LoadoutGroupFilesViewModel : APageViewModel<ILoadoutGroupFilesViewModel>, ILoadoutGroupFilesViewModel
{
    [Reactive] public LoadoutGroupFilesPageContext? Context { get; set; }
    [Reactive] public IFileTreeViewModel? FileTreeViewModel { get; [UsedImplicitly] private set; }
    [Reactive] public FileTreeNodeViewModel? SelectedItem { get; [UsedImplicitly] private set; }

    public ReactiveCommand<NavigationInformation, Unit> OpenEditorCommand { get; }

    public LoadoutGroupFilesViewModel(
        ILogger<LoadoutGroupFilesViewModel> logger,
        IWindowManager windowManager,
        IConnection connection) : base(windowManager)
    {
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
                    LoadoutFileId = loadoutFile,
                    FilePath = loadoutFile.AsLoadoutItemWithTargetPath().TargetPath,
                },
            };

            var workspaceController = windowManager.ActiveWorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            var workspaceId = workspaceController.ActiveWorkspaceId;
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        }, this.WhenAnyValue(vm => vm.SelectedItem).WhereNotNull().Select(x => x.IsFile));

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.Context)
                .WhereNotNull()
                .Select(context => LoadoutItemGroup.Load(connection.Db, context.GroupId))
                .Where(group => group.IsValid())
                .Select(group => new LoadoutGroupFileTreeViewModel(group))
                .BindToVM(this, vm => vm.FileTreeViewModel)
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
