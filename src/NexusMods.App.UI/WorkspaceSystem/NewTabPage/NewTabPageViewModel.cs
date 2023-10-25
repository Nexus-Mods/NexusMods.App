namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageViewModel : AViewModel<INewTabPageViewModel>, INewTabPageViewModel
{
    public INewTabPageSectionViewModel[] SectionViewModels { get; }

    public NewTabPageViewModel(INewTabPageSectionViewModel[] sectionViewModels)
    {
        SectionViewModels = sectionViewModels;
    }
}
