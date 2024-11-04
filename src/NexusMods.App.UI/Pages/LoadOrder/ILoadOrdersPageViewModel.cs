using NexusMods.App.UI.Pages.LoadOrder.Prototype;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LoadOrder;

public interface ILoadOrdersPageViewModel : IPageViewModelInterface
{
    ILoadOrderViewModel? LoadOrderViewModel { get; }
}
