using System.Windows.Input;

namespace NexusMods.App.UI.Overlays;

public interface INexusLoginOverlayViewModel : IOverlayViewModel
{
    public ICommand Cancel { get; }
    public Uri Uri { get; }
    public bool IsActive { get; }
}
