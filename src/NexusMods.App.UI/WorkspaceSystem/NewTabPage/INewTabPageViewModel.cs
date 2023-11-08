namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageViewModel : IViewModelInterface
{
    public INewTabPageSectionViewModel[] SectionViewModels { get; }
}
