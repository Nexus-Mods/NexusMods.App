using System.Windows.Input;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

public class MetricsOptInDesignerViewModel : AViewModel<IMetricsOptInViewModel>, IMetricsOptInViewModel
{
    public bool IsActive { get; set; }
    public ICommand Allow { get; }
    public ICommand Deny { get; }

    public MetricsOptInDesignerViewModel()
    {
        Allow = ReactiveCommand.Create(() =>
        {
            IsActive = false;
        });
        Deny = ReactiveCommand.Create(() =>
        {
            IsActive = false;
        });
    }
}
