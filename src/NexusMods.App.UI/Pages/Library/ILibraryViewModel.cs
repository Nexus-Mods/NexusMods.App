using Avalonia.Controls;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Library;

public interface ILibraryViewModel : IPageViewModelInterface
{
    ITreeDataGridSource<LibraryNode> Source { get; }
}
