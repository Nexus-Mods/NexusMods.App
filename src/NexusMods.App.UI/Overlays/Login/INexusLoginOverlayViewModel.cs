using System.Windows.Input;

namespace NexusMods.App.UI.Overlays.Login;

public interface INexusLoginOverlayViewModel : IOverlayViewModel
{
    public ICommand Cancel { get; }
    public Uri Uri { get; }
}
