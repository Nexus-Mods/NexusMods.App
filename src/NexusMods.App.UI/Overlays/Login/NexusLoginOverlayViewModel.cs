using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.NexusWebApi;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Login;

public class NexusLoginOverlayViewModel : AOverlayViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel
{
    public NexusLoginOverlayViewModel(IReadOnlyActivity activity)
    {
        Uri = (Uri)activity.Payload!;
        Cancel = ReactiveCommand.Create(() =>
            {
                ((IActivitySource)activity).Dispose();
                Close();
            }
        );
    }

    public ICommand Cancel { get; }

    public Uri Uri { get; }
}
