using System.Reactive;
using JetBrains.Annotations;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

[UsedImplicitly]
public class MarkdownRendererViewModel : AViewModel<IMarkdownRendererViewModel>, IMarkdownRendererViewModel
{
    [Reactive] public string Contents { get; set; } = string.Empty;

    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

    public MarkdownRendererViewModel(IOSInterop osInterop)
    {
        OpenLinkCommand = ReactiveCommand.CreateFromTask<string>(async url =>
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
            await Task.Run(() =>
            {
                osInterop.OpenUrl(uri);
            });
        });
    }
}
