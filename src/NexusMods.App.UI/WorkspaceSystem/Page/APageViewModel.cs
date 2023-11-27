using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public abstract class APageViewModel<TInterface> : AViewModel<TInterface>
    where TInterface : class, IPageViewModelInterface
{
    public ReactiveCommand<PageData, PageData> ChangePageCommand { get; } = ReactiveCommand.Create<PageData, PageData>(pageData => pageData);
}
