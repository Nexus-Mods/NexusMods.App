using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPageViewModelInterface : IViewModelInterface
{
    public ReactiveCommand<PageData, PageData> ChangePageCommand { get; }
}
