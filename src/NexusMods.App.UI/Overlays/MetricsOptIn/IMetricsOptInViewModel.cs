using System.Windows.Input;
using NexusMods.App.UI.Controls.MarkdownRenderer;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

public interface IMetricsOptInViewModel : IOverlayViewModel
{
    /// <summary>
    /// Command to call to allow metrics.
    /// </summary>
    public ICommand Allow { get; }

    /// <summary>
    /// Command to call to deny metrics.
    /// </summary>
    public ICommand Deny { get; }

    /// <summary>
    /// If the metrics opt-in overlay has not been shown before, then show it now. Returns true if the overlay was shown.
    /// </summary>
    public bool MaybeShow();
}
