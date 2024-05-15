using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Selection;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using File = NexusMods.Abstractions.Loadouts.Files.File;

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
            var key = SelectedItem!.Key;
            var targetModId = _modId.Value;

            // NOTE(erri120): not a huge fan of this, but it works
            var db = connection.Db;
            var storedFile = db
                .Find(StoredFile.Hash)
                .Select(id => db.Get<StoredFile.Model>(id))
                .FirstOrDefault(storedFile =>
                {
                    if (!targetModId.Equals(storedFile.ModId)) return false;
                    return storedFile.To.Equals(key);
                });

            if (storedFile is null) return;

            var pageData = new PageData
            {
                FactoryId = TextEditorPageFactory.StaticId,
                Context = new TextEditorPageContext
                {
                    FileId = storedFile.FileId,
                    FilePath = storedFile.To,
                },
            };

            if (!windowManager.TryGetActiveWindow(out var activeWindow)) return;
            var workspaceController = activeWindow.WorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info, _pageIdBundle);
            var workspaceId = workspaceController.ActiveWorkspace!.Id;
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
