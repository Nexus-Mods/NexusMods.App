using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager) { }
}
