using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Media;
using JetBrains.Annotations;
using Markdown.Avalonia.Plugins;
using Markdown.Avalonia.Utils;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

[UsedImplicitly]
public class MarkdownRendererViewModel : AViewModel<IMarkdownRendererViewModel>, IMarkdownRendererViewModel
{
    private readonly HttpClient _httpClient;

    [Reactive] public string Contents { get; set; } = string.Empty;

    [Reactive] public Uri? MarkdownUri { get; set; }

    public IMdAvPlugin ImageResolverPlugin { get; }
    public IPathResolver PathResolver { get; }
    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

    public MarkdownRendererViewModel(IOSInterop osInterop, HttpClient httpClient)
    {
        _httpClient = httpClient;

        PathResolver = new PathResolverImpl(this);
        ImageResolverPlugin = new ImageResolvePluginImpl(new ImageResolverImpl(this));

        OpenLinkCommand = ReactiveCommand.CreateFromTask<string>(async url =>
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
            await Task.Run(() =>
            {
                osInterop.OpenUrl(uri);
            });
        });

        var fetchMarkdownCommand = ReactiveCommand.CreateFromTask<Uri, string>(FetchMarkdown);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.MarkdownUri)
                .OnUI()
                .WhereNotNull()
                .OffUi()
                .InvokeCommand(fetchMarkdownCommand)
                .DisposeWith(disposables);

            fetchMarkdownCommand
                .OnUI()
                .BindToVM(this, vm => vm.Contents)
                .DisposeWith(disposables);
        });
    }

    private async Task<string> FetchMarkdown(Uri uri, CancellationToken cancellationToken = default)
    {
        var changelog = await _httpClient.GetStringAsync(uri, cancellationToken);
        return changelog;
    }

    private class ImageResolvePluginImpl : IMdAvPlugin
    {
        private readonly IImageResolver _imageResolver;

        public ImageResolvePluginImpl(IImageResolver imageResolver)
        {
            _imageResolver = imageResolver;
        }

        public void Setup(SetupInfo info)
        {
            info.Register(_imageResolver);
        }
    }

    private class PathResolverImpl : IPathResolver
    {
        private readonly MarkdownRendererViewModel _parent;

        public string? AssetPathRoot { get; set; }
        public IEnumerable<string>? CallerAssemblyNames { get; set; }

        public PathResolverImpl(MarkdownRendererViewModel parent)
        {
            _parent = parent;
        }

        public Task<Stream?>? ResolveImageResource(string relativeOrAbsolutePath)
        {
            return null;
        }
    }

    private class ImageResolverImpl : IImageResolver
    {
        private readonly MarkdownRendererViewModel _parent;
        public ImageResolverImpl(MarkdownRendererViewModel parent)
        {
            _parent = parent;
        }

        public Task<IImage?> Load(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
