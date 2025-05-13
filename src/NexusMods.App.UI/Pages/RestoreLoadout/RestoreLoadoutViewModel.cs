using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public class RestoreLoadoutViewModel : APageViewModel<IRestoreLoadoutViewModel>, IRestoreLoadoutViewModel
{
    public RestoreLoadoutViewModel(IWindowManager windowManager) : base(windowManager)
    {
    }

    [Reactive]
    public LoadoutId LoadoutId { get; set; }
}
