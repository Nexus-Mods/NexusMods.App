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
## Help us improve

Help us provide you with the best modding experience.

With your permission, we will collect anonymous analytics information and send it to our team to help us improve quality and performance.

[More information about the data we track]({ TelemetrySettings.Link })
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
