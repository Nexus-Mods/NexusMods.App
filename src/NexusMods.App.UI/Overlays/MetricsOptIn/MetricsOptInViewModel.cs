using System.Windows.Input;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

/// <summary>
/// Primary view model for the MetricsOptIn overlay.
/// </summary>
public class MetricsOptInViewModel : AViewModel<IMetricsOptInViewModel>, IMetricsOptInViewModel
{
    private readonly IOverlayController _overlayController;
    private readonly ISettingsManager _settingsManager;

    [Reactive]
    public bool IsActive { get; set; }
    public ICommand Allow { get; }
    public ICommand Deny { get; }

    /// <summary>
    /// DI Constructor
    /// </summary>
    public MetricsOptInViewModel(ISettingsManager settingsManager, IOverlayController overlayController)
    {
        _overlayController = overlayController;
        _settingsManager = settingsManager;

        Allow = ReactiveCommand.Create(() =>
        {
            var current = _settingsManager.Get<TelemetrySettings>();
            var updated = current with
            {
                EnableTelemetry = true,
                HasShownPrompt = true,
            };

            _settingsManager.Set(updated);
            IsActive = false;
        });

        Deny = ReactiveCommand.Create(() =>
        {
            var current = _settingsManager.Get<TelemetrySettings>();
            var updated = current with
            {
                EnableTelemetry = false,
                HasShownPrompt = true,
            };

            _settingsManager.Set(updated);
            IsActive = false;
        });
    }

    public bool MaybeShow()
    {
        if (_settingsManager.Get<TelemetrySettings>().HasShownPrompt) return false;

        _overlayController.SetOverlayContent(new SetOverlayItem(this));
        return true;
    }
}
