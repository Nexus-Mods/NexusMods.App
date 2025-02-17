using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree;

public interface IItemContentsFileTreeViewModel : IPageViewModelInterface
{
    ItemContentsFileTreePageContext? Context { get; set; }

    IFileTreeViewModel? FileTreeViewModel { get; }

    R3.ReactiveCommand<NavigationInformation> OpenEditorCommand { get; }
    
    R3.ReactiveCommand<R3.Unit> RemoveCommand { get; }
}
