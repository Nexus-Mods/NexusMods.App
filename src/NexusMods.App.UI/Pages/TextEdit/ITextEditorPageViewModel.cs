using AvaloniaEdit.Document;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.TextEdit;

public interface ITextEditorPageViewModel : IPageViewModelInterface
{
    /// <summary>
    /// Gets or sets whether the text editor is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the contents in the text editor have been modified.
    /// </summary>
    public bool IsModified { get; set; }

    /// <summary>
    /// Gets or sets the document to display and edit in the text editor.
    /// </summary>
    public TextDocument? Document { get; set; }
}
