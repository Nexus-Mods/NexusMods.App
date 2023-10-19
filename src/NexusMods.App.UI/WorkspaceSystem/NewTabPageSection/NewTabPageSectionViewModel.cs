namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionViewModel : AViewModel<INewTabPageSectionViewModel>, INewTabPageSectionViewModel
{
    public string SectionName { get; }

    public NewTabPageSectionViewModel(string sectionName)
    {
        SectionName = sectionName;
    }
}
