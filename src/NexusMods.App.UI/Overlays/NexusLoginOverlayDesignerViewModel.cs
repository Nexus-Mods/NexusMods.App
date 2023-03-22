using System.Windows.Input;

namespace NexusMods.App.UI.Overlays;

public class NexusLoginOverlayDesignerViewModel : AViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel
{
    public ICommand Cancel { get; } = Initializers.ICommand;
    public Uri Uri { get; } = new("https://www.nexusmods.com/some/login?name=John&key=1234567890");
    public bool IsActive { get; } = true;
}
