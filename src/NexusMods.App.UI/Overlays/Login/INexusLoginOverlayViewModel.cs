namespace NexusMods.App.UI.Overlays.Login;

public interface INexusLoginOverlayViewModel : IOverlayViewModel
{
    public R3.ReactiveCommand Cancel { get; }
    public Uri? Uri { get; }
}
