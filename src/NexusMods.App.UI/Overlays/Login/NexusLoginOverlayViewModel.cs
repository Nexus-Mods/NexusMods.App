using System.Windows.Input;

namespace NexusMods.App.UI.Overlays.Login;

public class NexusLoginOverlayViewModel : AOverlayViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel
{
    public NexusLoginOverlayViewModel()
    {
        // TODO:
        Uri = null!;
        Cancel = null!;

        // Uri = (Uri)activity.Payload!;
        // Cancel = ReactiveCommand.Create(() =>
        //     {
        //         if (activity is IActivitySource activitySource)
        //             activitySource.Dispose();
        //         Close();
        //     }
        // );
    }

    public ICommand Cancel { get; }

    public Uri Uri { get; }
}
