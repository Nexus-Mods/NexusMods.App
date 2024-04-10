using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

/// <summary>
/// Primary view model for the MetricsOptIn overlay.
/// </summary>
public class MetricsOptInViewModel : AViewModel<IMetricsOptInViewModel>, IMetricsOptInViewModel
{
    private readonly IOverlayController _overlayController;

    [Reactive]
    public bool IsActive { get; set; }
    public ICommand Allow { get; }
    public ICommand Deny { get; }

    /// <summary>
    /// DI Constructor
    /// </summary>
    public MetricsOptInViewModel(IOverlayController overlayController)
    {
        _overlayController = overlayController;
        // _globalSettingsManager = globalSettingsManager;
        Allow = ReactiveCommand.Create(() =>
        {
            // TODO:
            // globalSettingsManager.SetMetricsOptIn(true);
            IsActive = false;
        });

        Deny = ReactiveCommand.Create(() =>
        {
            // globalSettingsManager.SetMetricsOptIn(false);
            IsActive = false;
        });
    }

    public bool MaybeShow()
    {
        // TODO:
        // if (_globalSettingsManager.IsMetricsOptInSet())
        //     return false;

        _overlayController.SetOverlayContent(new SetOverlayItem(this));
        return true;
    }
}
