using NexusMods.App.UI.Overlays;

namespace NexusMods.App.UI.Windows;

public interface IMainWindowViewModel : IViewModelInterface, IWorkspaceWindow
{
    IOverlayViewModel? CurrentOverlay { get; }
}
