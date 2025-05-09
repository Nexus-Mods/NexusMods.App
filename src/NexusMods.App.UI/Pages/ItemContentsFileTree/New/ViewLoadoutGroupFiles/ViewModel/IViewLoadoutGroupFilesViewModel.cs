using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.View;
using NexusMods.App.UI.WorkspaceSystem;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel;

public interface IViewLoadoutGroupFilesViewModel : IPageViewModelInterface
{
    ViewLoadoutGroupFilesPageContext? Context { get; set; }

    R3.ReactiveCommand<NavigationInformation> OpenEditorCommand { get; }

    R3.ReactiveCommand<R3.Unit> RemoveCommand { get; }
    
    ViewLoadoutGroupFilesTreeDataGridAdapter? FileTreeAdapter { get; set; }
}
