namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionViewModel : IViewModelInterface
{
    public string SectionName { get; }

    public INewTabPageSectionItemViewModel[] Items { get; }
}
