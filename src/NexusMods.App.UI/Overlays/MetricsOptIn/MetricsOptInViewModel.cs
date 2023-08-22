using System.Windows.Input;
using NexusMods.DataModel.GlobalSettings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

/// <summary>
/// Primary view model for the MetricsOptIn overlay.
/// </summary>
public class MetricsOptInViewModel : AViewModel<IMetricsOptInViewModel>, IMetricsOptInViewModel
{
    [Reactive]
    public bool IsActive { get; set; }
    public ICommand Allow { get; }
    public ICommand Deny { get; }

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="globalSettingsManager"></param>
    public MetricsOptInViewModel(GlobalSettingsManager globalSettingsManager)
    {
        Allow = ReactiveCommand.Create(() =>
        {
            globalSettingsManager.SetMetricsOptIn(true);
            IsActive = false;
        });

        Deny = ReactiveCommand.Create(() =>
        {
            globalSettingsManager.SetMetricsOptIn(false);
            IsActive = false;
        });
    }
}
