using NexusMods.App.UI.Overlays;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Windows;

public interface IMainWindowViewModel : IViewModelInterface, IWorkspaceWindow
{
    IOverlayViewModel? CurrentOverlay { get; }
}
