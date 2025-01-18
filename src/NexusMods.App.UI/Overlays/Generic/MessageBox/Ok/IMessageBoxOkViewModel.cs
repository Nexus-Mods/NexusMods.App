using NexusMods.App.UI.Controls.MarkdownRenderer;
using R3;
namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;

/// <summary>
/// Interface for a message box model with an 'ok' button.
/// </summary>
public interface IMessageBoxOkViewModel : IOverlayViewModel<Unit>
{
    /// <summary>
    /// A short title for the message box.
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// A description of what's happening.
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// If provided, this will be displayed in a markdown control below the description. Use this
    /// for more descriptive information.
    /// </summary>
    public IMarkdownRendererViewModel? MarkdownRenderer { get; set; }
}
