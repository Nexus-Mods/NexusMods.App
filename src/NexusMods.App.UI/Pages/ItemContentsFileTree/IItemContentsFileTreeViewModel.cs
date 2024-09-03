using System.Reactive;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree;

public interface IItemContentsFileTreeViewModel : IPageViewModelInterface
{
    ItemContentsFileTreePageContext? Context { get; set; }

    IFileTreeViewModel? FileTreeViewModel { get; }

    ReactiveCommand<NavigationInformation, Unit> OpenEditorCommand { get; }
}
