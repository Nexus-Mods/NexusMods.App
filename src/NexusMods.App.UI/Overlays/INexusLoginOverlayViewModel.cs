using System.Windows.Input;

namespace NexusMods.App.UI.Overlays;

public interface INexusLoginOverlayViewModel : IViewModelInterface
{
    public ICommand Cancel { get; }
    public Uri Uri { get; }
}
