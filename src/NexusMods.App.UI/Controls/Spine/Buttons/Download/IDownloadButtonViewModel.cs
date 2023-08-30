using System.Windows.Input;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public interface IDownloadButtonViewModel : IViewModelInterface
{
    /// <summary>
    /// The number to display for example 8.5 for 8.5 MB/s
    /// </summary>
    public float Number { get; }
    
    /// <summary>
    /// The units to display for example MB/s
    /// </summary>
    public string Units { get; }
    
    /// <summary>
    /// The progress of the downloads
    /// </summary>
    public Percent? Progress { get; }
    
    /// <summary>
    /// Command to execute when the button is clicked
    /// </summary>
    public ICommand Click { get; set; }
    
    /// <summary>
    /// True when this spine button is active
    /// </summary>
    public bool IsActive { get; set; }
}
