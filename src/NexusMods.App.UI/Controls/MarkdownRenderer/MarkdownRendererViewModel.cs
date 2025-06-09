using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Media;
using JetBrains.Annotations;
using Markdown.Avalonia.Plugins;
using Markdown.Avalonia.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.Sdk.Resources;
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

    [Reactive]
    [LanguageInjection("markdown")]
    public string Contents { get; set; } = string.Empty;

    [Reactive] public Uri? MarkdownUri { get; set; }
    private Uri? _gitHubBaseUri;

    public IMdAvPlugin ImageResolverPlugin { get; }
    public IPathResolver PathResolver { get; }
    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

    public MarkdownRendererViewModel()
    {
        DesignerUtils.AssertInDesigner();

        _logger = NullLogger.Instance;
        _httpClient = new HttpClient();
        _remoteImagePipeline = ImagePipelines.CreateMarkdownRendererRemoteImagePipeline(_httpClient);

        PathResolver = new PathResolverImpl(this);
        ImageResolverPlugin = new ImageResolvePluginImpl(new ImageResolverImpl(this));

        OpenLinkCommand = ReactiveCommand.Create<string>(_ => { });
    }

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

    /// <summary>
    /// From https://github.com/adam-p/markdown-here/wiki/Markdown-Cheatsheet
    /// </summary>
    [LanguageInjection("markdown")]
    public const string DebugText =
"""
# H1
## H2
### H3
#### H4
##### H5
###### H6

Alternatively, for H1 and H2, an underline-ish style:

Alt-H1
======

Alt-H2
------

Emphasis, aka italics, with *asterisks* or _underscores_.

Strong emphasis, aka bold, with **asterisks** or __underscores__.

Combined emphasis with **asterisks and _underscores_**.

Strikethrough uses two tildes. ~~Scratch this.~~

1. First ordered list item
2. Another item
⋅⋅* Unordered sub-list. 
1. Actual numbers don't matter, just that it's a number
⋅⋅1. Ordered sub-list
4. And another item.

⋅⋅⋅You can have properly indented paragraphs within list items. Notice the blank line above, and the leading spaces (at least one, but we'll use three here to also align the raw Markdown).

⋅⋅⋅To have a line break without a paragraph, you will need to use two trailing spaces.⋅⋅
⋅⋅⋅Note that this line is separate, but within the same paragraph.⋅⋅
⋅⋅⋅(This is contrary to the typical GFM line break behaviour, where trailing spaces are not required.)

* Unordered list can use asterisks
- Or minuses
+ Or pluses

[I'm an inline-style link](https://www.google.com)

[I'm an inline-style link with title](https://www.google.com "Google's Homepage")

[I'm a reference-style link][Arbitrary case-insensitive reference text]

[I'm a relative reference to a repository file](../blob/master/LICENSE)

[You can use numbers for reference-style link definitions][1]

Or leave it empty and use the [link text itself].

URLs and URLs in angle brackets will automatically get turned into links. 
http://www.example.com or <http://www.example.com> and sometimes 
example.com (but not on Github, for example).

Some text to show that the reference links can follow later.

[arbitrary case-insensitive reference text]: https://www.mozilla.org
[1]: http://slashdot.org
[link text itself]: http://www.reddit.com

Here's our logo (hover to see the title text):

Inline-style: 
![alt text](https://github.com/adam-p/markdown-here/raw/master/src/common/images/icon48.png "Logo Title Text 1")

Reference-style: 
![alt text][logo]

[logo]: https://github.com/adam-p/markdown-here/raw/master/src/common/images/icon48.png "Logo Title Text 2"

Inline `code` has `back-ticks around` it.

```javascript
var s = "JavaScript syntax highlighting";
alert(s);
```
 
```python
s = "Python syntax highlighting"
print s
```
 
```
No language indicated, so no syntax highlighting. 
But let's throw in a <b>tag</b>.
```

Here is a simple footnote[^1].

A footnote can also have multiple lines[^2].  

You can also use words, to fit your writing style more closely[^note].

[^1]: My reference.
[^2]: Every new line should be prefixed with 2 spaces.  
  This allows you to have a footnote with multiple lines.
[^note]:
    Named footnotes will still render with numbers instead of the text but allow easier identification and linking.  
    This footnote also has been made with a different syntax using 4 spaces for new lines.

Colons can be used to align columns.

| Tables        | Are           | Cool  |
| ------------- |:-------------:| -----:|
| col 3 is      | right-aligned | $1600 |
| col 2 is      | centered      |   $12 |
| zebra stripes | are neat      |    $1 |

There must be at least 3 dashes separating each header cell.
The outer pipes (|) are optional, and you don't need to make the 
raw Markdown line up prettily. You can also use inline Markdown.

Markdown | Less | Pretty
--- | --- | ---
*Still* | `renders` | **nicely**
1 | 2 | 3

> Blockquotes are very handy in email to emulate reply text.
> This line is part of the same quote.

Quote break.

> This is a very long line that will still be quoted properly when it wraps. Oh boy let's keep writing to make sure this is long enough to actually wrap for everyone. Oh, you can *put* **Markdown** into a blockquote. 

<dl>
  <dt>Definition list</dt>
  <dd>Is something people use sometimes.</dd>

  <dt>Markdown in HTML</dt>
  <dd>Does *not* work **very** well. Use HTML <em>tags</em>.</dd>
</dl>

Three or more...

---

Hyphens

***

Asterisks

___

Underscores

Here's a line for us to start with.

This line is separated from the one above by two newlines, so it will be a *separate paragraph*.

This line is also a separate paragraph, but...
This line is only separated by a single newline, so it's a separate line in the *same paragraph*.
""";
}
