using NexusMods.App.UI.Controls;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageDesignViewModel : NewTabPageViewModel
{
    public NewTabPageDesignViewModel() : base(CreateDesignData()) { }

    private static PageDiscoveryDetails[] CreateDesignData()
    {
        return Enumerable
            .Range(1, 9)
            .Select(i => new PageDiscoveryDetails
            {
                SectionName = $"Section {i % 3}",
                ItemName = $"Item {i}",
                PageData = new PageData
                {
                    FactoryId = DummyPageFactory.StaticId,
                    Context = new DummyPageContext()
                }
            })
            .ToArray();
    }
}
