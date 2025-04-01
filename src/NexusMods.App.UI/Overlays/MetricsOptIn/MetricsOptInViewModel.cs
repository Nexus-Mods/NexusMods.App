using System.Windows.Input;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

/// <summary>
/// Primary view model for the MetricsOptIn overlay.
/// </summary>
[UsedImplicitly]
public class MetricsOptInViewModel : AOverlayViewModel<IMetricsOptInViewModel>, IMetricsOptInViewModel
{
    private readonly ISettingsManager _settingsManager;
    public ICommand Allow { get; }
    public ICommand Deny { get; }

    /// <summary>
    /// DI Constructor
    /// </summary>
    public MetricsOptInViewModel(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        
        Allow = ReactiveCommand.Create(() =>
        {
            _settingsManager.Update<TelemetrySettings>(current => current with
            {
                IsEnabled = true,
                HasShownPrompt = true,
            });

            Close();
        });

        Deny = ReactiveCommand.Create(() =>
        {
            _settingsManager.Update<TelemetrySettings>(current => current with
            {
                IsEnabled = false,
                HasShownPrompt = true,
            });

            Close();
        });
    }

    public bool MaybeShow()
    {
        if (_settingsManager.Get<TelemetrySettings>().HasShownPrompt) return false;

        Controller.Enqueue(this);
        return true;
    }
}
