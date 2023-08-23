using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

public class MetricsOptInDesignerViewModel : AViewModel<IMetricsOptInViewModel>, IMetricsOptInViewModel
{
    [Reactive]
    public bool IsActive { get; set; }

    [Reactive]
    public ICommand Allow { get; set; }

    [Reactive]
    public ICommand Deny { get; set; }

    [Reactive]
    public bool AllowClicked { get; set; }

    [Reactive]
    public bool DenyClicked { get; set; }

    public MetricsOptInDesignerViewModel()
    {
        Allow = ReactiveCommand.Create(() =>
        {
            AllowClicked = true;
            IsActive = false;
        });
        Deny = ReactiveCommand.Create(() =>
        {
            DenyClicked = true;
            IsActive = false;
        });
    }
    public void MaybeShow()
    {
        throw new NotImplementedException();
    }
}
