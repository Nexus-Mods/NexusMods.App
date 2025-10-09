using System.Reactive;
using JetBrains.Annotations;
using Markdown.Avalonia.Plugins;
using Markdown.Avalonia.Utils;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

public interface IMarkdownRendererViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets or sets the contents of the renderer.
    /// </summary>
    [LanguageInjection(injectedLanguage: "markdown")]
    public string Contents { get; set; }

    /// <summary>
    /// Gets or sets the Uri of the contents.
    /// </summary>
    /// <remarks>
    /// This will fetch the markdown and set <see cref="Contents"/>.
    /// </remarks>
    public Uri? MarkdownUri { get; set; }

    public IMdAvPlugin ImageResolverPlugin { get; }
    public IPathResolver PathResolver { get; }
    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }
}
