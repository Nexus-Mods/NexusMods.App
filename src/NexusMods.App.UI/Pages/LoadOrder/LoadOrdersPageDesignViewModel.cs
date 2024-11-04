using NexusMods.App.UI.Pages.LoadOrder.Prototype;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LoadOrder;

public class LoadOrdersPageDesignViewModel : APageViewModel<ILoadOrdersPageViewModel>, ILoadOrdersPageViewModel
{
    public LoadOrdersPageDesignViewModel() : base(new DesignWindowManager())
    {
    }

    public ILoadOrderViewModel? LoadOrderViewModel { get; }
}
