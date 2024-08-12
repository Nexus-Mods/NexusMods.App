using NexusMods.Abstractions.Games.DTO;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Library;

public interface ILibraryViewModel : IPageViewModelInterface
{
    GameDomain GameDomain { get; }
}
