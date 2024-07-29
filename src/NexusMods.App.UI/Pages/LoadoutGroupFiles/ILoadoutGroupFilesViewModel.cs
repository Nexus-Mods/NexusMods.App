using System.Reactive;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGroupFiles;

public interface ILoadoutGroupFilesViewModel : IPageViewModelInterface
{
    LoadoutGroupFilesPageContext? Context { get; set; }

    IFileTreeViewModel? FileTreeViewModel { get; }

    ReactiveCommand<NavigationInformation, Unit> OpenEditorCommand { get; }
}
