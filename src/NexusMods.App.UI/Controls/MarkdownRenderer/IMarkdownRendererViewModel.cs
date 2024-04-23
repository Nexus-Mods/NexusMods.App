using System.Reactive;
using Markdown.Avalonia.Utils;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

public interface IMarkdownRendererViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets or sets the contents of the renderer.
    /// </summary>
    public string Contents { get; set; }

    /// <summary>
    /// Gets the command used for opening links from Markdown.
    /// </summary>
    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

    public IPathResolver PathResolver { get; }

    public IImageResolver ImageResolver { get; }
}
