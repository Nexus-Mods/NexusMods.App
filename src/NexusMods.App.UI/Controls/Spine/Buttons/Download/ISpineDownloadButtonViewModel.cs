using System.Windows.Input;
using DynamicData.Kernel;
using NexusMods.Abstractions.Activities;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public interface ISpineDownloadButtonViewModel : IViewModelInterface
{
    /// <summary>
    /// The number to display for example 8.5 for 8.5 MB/s
    /// </summary>
    public double Number { get; }

    /// <summary>
    /// The units to display for example MB/s
    /// </summary>
    public string Units { get; }

    /// <summary>
    /// The progress of the downloads
    /// </summary>
    public Optional<Percent> Progress { get; }

    /// <summary>
    /// Command to execute when the button is clicked
    /// </summary>
    public ICommand Click { get; set; }

    /// <summary>
    /// True when this spine button is active
    /// </summary>
    public bool IsActive { get; set; }
}
