using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Media;
using JetBrains.Annotations;
using Markdown.Avalonia.Plugins;
using Markdown.Avalonia.Utils;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Extensions;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

[UsedImplicitly]
public class MarkdownRendererViewModel : AViewModel<IMarkdownRendererViewModel>, IMarkdownRendererViewModel
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IResourceLoader<Uri, IImage> _remoteImagePipeline;

    [Reactive] public string Contents { get; set; } = string.Empty;
    [Reactive] public Uri? MarkdownUri { get; set; }
    private Uri? _gitHubBaseUri;

    public IMdAvPlugin ImageResolverPlugin { get; }
    public IPathResolver PathResolver { get; }
    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

    public MarkdownRendererViewModel(
        IServiceProvider serviceProvider,
        ILogger<MarkdownRendererViewModel> logger,
        IOSInterop osInterop,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _remoteImagePipeline = ImagePipelines.GetMarkdownRendererRemoteImagePipeline(serviceProvider);

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
                .Do(ParseGitHubUri)
                .WhereNotNull()
                .OffUi()
                .InvokeReactiveCommand(fetchMarkdownCommand)
                .DisposeWith(disposables);

            fetchMarkdownCommand
                .OnUI()
                .BindToVM(this, vm => vm.Contents)
                .DisposeWith(disposables);
        });
    }

    private void ParseGitHubUri(Uri? markdownUri)
    {
        // https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/main/CHANGELOG.md
        // https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/main/docs/changelog-assets/081da2f32c8803bbd759cf2f22641810.webp

        _gitHubBaseUri = null;
        if (markdownUri is null) return;

        var url = markdownUri.ToString();
        var span = url.AsSpan();

        const string prefix = "https://raw.githubusercontent.com/";
        if (!span.StartsWith(prefix)) return;

        var slice = span.Slice(prefix.Length);
        var slashIndex = slice.IndexOf('/');
        if (slashIndex == -1) return;

        var user = slice.Slice(start: 0, length: slashIndex).ToString();
        slice = slice.Slice(slashIndex + 1);

        slashIndex = slice.IndexOf('/');
        if (slashIndex == -1) return;

        var repo = slice.Slice(start: 0, length: slashIndex).ToString();
        slice = slice.Slice(slashIndex + 1);

        slashIndex = slice.IndexOf('/');
        if (slashIndex == -1) return;

        var branch = slice.Slice(start: 0, length: slashIndex).ToString();
        _gitHubBaseUri = new Uri($"{prefix}{user}/{repo}/{branch}/");
    }

    private async Task<string> FetchMarkdown(Uri uri, CancellationToken cancellationToken = default)
    {
        var changelog = await _httpClient.GetStringAsync(uri, cancellationToken);
        return changelog;
    }

    private async Task<Stream?> FetchGitHubImage(string path, CancellationToken cancellationToken = default)
    {
        if (_gitHubBaseUri is null) return null;
        if (path.StartsWith("./")) path = path.Substring(startIndex: 2);

        var uri = new Uri($"{_gitHubBaseUri}{path}");
        return await FetchRemoteImage(uri, cancellationToken);
    }

    private Task<Stream?> FetchRemoteImage(Uri uri, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(uri.ToString());
        var ms = new MemoryStream(bytes, writable: false);
        return Task.FromResult<Stream?>(ms);
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
            if (Uri.TryCreate(relativeOrAbsolutePath, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == "http")
                {
                    _parent._logger.LogWarning("Skip loading image from unsecure HTTP URL: {Uri}", uri);
                    return null;
                }

                if (uri.Scheme != "https")
                {
                    _parent._logger.LogWarning("Unknown URI schema: {Uri}", uri);
                    return null;
                }

                return _parent.FetchRemoteImage(uri);
            }

            if (_parent.MarkdownUri is null) return null;
            return _parent.FetchGitHubImage(relativeOrAbsolutePath);
        }
    }

    private class ImageResolverImpl : IImageResolver
    {
        private readonly MarkdownRendererViewModel _parent;
        public ImageResolverImpl(MarkdownRendererViewModel parent)
        {
            _parent = parent;
        }

        public async Task<IImage?> Load(Stream stream)
        {
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var url = await sr.ReadToEndAsync();
            var uri = new Uri(url, UriKind.Absolute);

            var resource = await _parent._remoteImagePipeline.LoadResourceAsync(uri, CancellationToken.None);
            return resource.Data;
        }
    }
}
