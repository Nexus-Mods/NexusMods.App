namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionDesignViewModel : NewTabPageSectionViewModel
{
    public NewTabPageSectionDesignViewModel() : this("Category Name", CreateItems()) { }

    public NewTabPageSectionDesignViewModel(string sectionName, INewTabPageSectionItemViewModel[] items) : base(sectionName, items) { }

    public static INewTabPageSectionItemViewModel[] CreateItems()
    {
        return Enumerable
            .Range(0, 3)
            .Select(i => (INewTabPageSectionItemViewModel)new NewTabPageSectionItemDesignViewModel($"Item {i}", icon: null))
            .ToArray();
    }
}
