using System.Windows.Input;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Icon;

public interface IIconButtonViewModel : IViewModelInterface
{
    /// <summary>
    /// Is the button active?
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Command to execute when the button is clicked.
    /// </summary>
    public ICommand Click { get; set; }

    /// <summary>
    /// Name for the tooltip on the button.
    /// </summary>
    public string Name { get; set; }

}
