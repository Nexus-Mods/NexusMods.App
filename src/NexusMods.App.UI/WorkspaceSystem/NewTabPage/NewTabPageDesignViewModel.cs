namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageDesignViewModel : NewTabPageViewModel
{
    public NewTabPageDesignViewModel() : this(CreateDesignData()) { }

    public NewTabPageDesignViewModel(INewTabPageSectionViewModel[] sectionViewModels) : base(sectionViewModels) { }

    private static INewTabPageSectionViewModel[] CreateDesignData()
    {
        return Enumerable
            .Range(0, 3)
            .Select(i => (INewTabPageSectionViewModel)new NewTabPageSectionDesignViewModel($"Section {i}"))
            .ToArray();
    }
}
