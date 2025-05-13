using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.RestoreLoadout;


public interface IRestoreLoadoutViewModel : IPageViewModelInterface
{
    public LoadoutId LoadoutId { get; set; }
}
