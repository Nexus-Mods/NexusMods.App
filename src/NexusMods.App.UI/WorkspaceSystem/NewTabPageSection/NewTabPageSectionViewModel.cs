namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionViewModel : AViewModel<INewTabPageSectionViewModel>, INewTabPageSectionViewModel
{
    public string SectionName { get; }

    public INewTabPageSectionItemViewModel[] Items { get; }

    public NewTabPageSectionViewModel(string sectionName, INewTabPageSectionItemViewModel[] items)
    {
        SectionName = sectionName;
        Items = items;
    }
}
