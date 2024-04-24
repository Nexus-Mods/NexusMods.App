using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModLibrary;

public interface IFileOriginsPageViewModel : IPageViewModelInterface
{
    LoadoutId LoadoutId { get; set; }
}
