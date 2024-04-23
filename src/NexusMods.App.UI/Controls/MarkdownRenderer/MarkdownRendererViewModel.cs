using System.Reactive;
using Avalonia.Media;
using JetBrains.Annotations;
using Markdown.Avalonia.Utils;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

[UsedImplicitly]
public class MarkdownRendererViewModel : AViewModel<IMarkdownRendererViewModel>, IMarkdownRendererViewModel
{
    [Reactive] public string Contents { get; set; } = string.Empty;

    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

    public IPathResolver PathResolver { get; }

    public IImageResolver ImageResolver { get; }

    public MarkdownRendererViewModel(IOSInterop osInterop)
    {
        PathResolver = new PathResolverImpl();
        ImageResolver = new ImageResolverImpl();

        OpenLinkCommand = ReactiveCommand.CreateFromTask<string>(async url =>
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
            await Task.Run(() =>
            {
                osInterop.OpenUrl(uri);
            });
        });
    }

    private class PathResolverImpl : IPathResolver
    {
        public string? AssetPathRoot { get; set; }
        public IEnumerable<string>? CallerAssemblyNames { get; set; }

        public Task<Stream?>? ResolveImageResource(string relativeOrAbsolutePath)
        {
            throw new NotImplementedException();
        }
    }

    private class ImageResolverImpl : IImageResolver
    {
        public Task<IImage?> Load(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
