using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Selection;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
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

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

[UsedImplicitly]
public class ModFilesViewModel : AViewModel<IModFilesViewModel>, IModFilesViewModel
{
    private readonly IConnection _connection;

    [Reactive] public IFileTreeViewModel? FileTreeViewModel { get; private set; }

    [Reactive] private IFileTreeNodeViewModel? SelectedItem { get; set; }

    public ReactiveCommand<NavigationInformation, Unit> OpenEditorCommand { get; }

    private Optional<LoadoutId> _loadoutId;
    private Optional<ModId> _modId;
    private Optional<PageIdBundle> _pageIdBundle;

    public ModFilesViewModel(IWindowManager windowManager, IConnection connection)
    {
        _connection = connection;

        OpenEditorCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            // TODO: rework this and only use LoadoutFile

            var key = SelectedItem!.Key;
            var targetModId = _modId.Value;

            var foundFile = Mod.Load(connection.Db, targetModId)
                .Files
                .OfTypeStoredFile()
                .TryGetFirst(x => x.AsFile().To.Equals(key), out var storedFile);

            if (!foundFile) return;
            if (!LoadoutFile.FindByHash(connection.Db, storedFile.Hash).TryGetFirst(out var loadoutFile)) return;

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
        }, this.WhenAnyValue(vm => vm.SelectedItem).WhereNotNull().Select(item => item.IsFile));

        this.WhenActivated(disposables =>
        {
            var serialDisposable = new SerialDisposable();

            this.WhenAnyValue(vm => vm.FileTreeViewModel!.TreeSource.Selection)
                .WhereNotNull()
                .SubscribeWithErrorLogging(selection =>
                {
                    if (selection is not ITreeDataGridRowSelectionModel selectionModel)
                    {
                        serialDisposable.Disposable = null;
                        SelectedItem = null;
                        return;
                    }

                    serialDisposable.Disposable = selectionModel.WhenAnyValue(x => x.SelectedItem)
                        .WhereNotNull()
                        .Cast<FileTreeNodeViewModel>()
                        .BindToVM(this, vm => vm.SelectedItem);
                })
                .DisposeWith(disposables);

            serialDisposable.DisposeWith(disposables);
        });
    }

    public void Initialize(LoadoutId loadoutId, ModId modId, PageIdBundle pageIdBundle)
    {
        _loadoutId = loadoutId;
        _modId = modId;
        _pageIdBundle = pageIdBundle;

        FileTreeViewModel = new ModFileTreeViewModel(loadoutId, modId, _connection);
    }
}
