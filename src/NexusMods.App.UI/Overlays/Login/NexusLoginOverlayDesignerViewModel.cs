namespace NexusMods.App.UI.Overlays.Login;

public class NexusLoginOverlayDesignerViewModel : AOverlayViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel
{
    public R3.ReactiveCommand Cancel { get; } = new();
    public Uri Uri { get; } = new("https://www.nexusmods.com/some/login?name=John&key=1234567890");
}
