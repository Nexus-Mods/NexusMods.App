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

    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    /// <summary>
    /// DI Constructor
    /// </summary>
    public MetricsOptInViewModel(ISettingsManager settingsManager, IMarkdownRendererViewModel markdownRendererViewModel)
    {
        _settingsManager = settingsManager;
        MarkdownRendererViewModel = markdownRendererViewModel;

        MarkdownRendererViewModel.Contents = $"""
## Diagnostics and Usage Data

We’d like to collect diagnostics and usage data to improve performance and enhance your experience. This data helps us identify issues, optimize features, and ensure the Nexus Mods app works better for everyone.

Your data will be processed in accordance with our [Privacy Policy]({ TelemetrySettings.Link }). You can change your preference anytime in settings.

Would you like to enable data collection?
""";

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
