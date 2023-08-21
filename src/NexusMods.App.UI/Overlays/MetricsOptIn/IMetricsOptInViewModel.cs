using System.Windows.Input;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

public interface IMetricsOptInViewModel : IOverlayViewModel
{
    public ICommand Allow { get; }
    public ICommand Deny { get; }
}
