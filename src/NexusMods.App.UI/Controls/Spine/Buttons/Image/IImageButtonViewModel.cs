using System.Windows.Input;
using Avalonia.Media;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Image;

public interface IImageButtonViewModel : IViewModelInterface
{
    /// <summary>
    /// Is the button active and highlighted.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Name for the tooltip on the button
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Image for the button
    /// </summary>
    public IImage Image { get; set; }

    /// <summary>
    /// Command to execute when the button is clicked
    /// </summary>
    public ICommand Click { get; set; }

    /// <summary>
    /// User defined data
    /// </summary>
    public object Tag { get; set; }
}
