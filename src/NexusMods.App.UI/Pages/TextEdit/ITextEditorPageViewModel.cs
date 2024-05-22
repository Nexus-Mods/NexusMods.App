using System.Reactive;
using AvaloniaEdit.Document;
using JetBrains.Annotations;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using TextMateSharp.Grammars;

namespace NexusMods.App.UI.Pages.TextEdit;

public interface ITextEditorPageViewModel : IPageViewModelInterface
{
    /// <summary>
    /// Gets or sets the current context of the page.
    /// </summary>
    public TextEditorPageContext? Context { get; set; }

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
    public TextDocument Document { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets the command for saving the document.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>
    /// Gets or sets the editor theme.
    /// </summary>
    public ThemeName Theme { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the editor font size.
    /// </summary>
    public double FontSize { get; [UsedImplicitly] set; }
}
