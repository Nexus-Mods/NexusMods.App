using DynamicData.Kernel;
using NexusMods.Sdk.Jobs;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public interface ISpineDownloadButtonViewModel : ISpineItemViewModel
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
    /// Text to display when hovering over the button
    /// </summary>
    public string ToolTip { get; set; }
}
