using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Controls;

public class DummyPage : IPage
{
    public IViewModel? ViewModel { get; set; } = new DummyViewModel();

    public required APageData PageData { get; set; }
}
