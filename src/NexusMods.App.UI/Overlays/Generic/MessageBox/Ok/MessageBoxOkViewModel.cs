using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Resources;
using R3;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;

public class MessageBoxOkViewModel : AOverlayViewModel<IMessageBoxOkViewModel, Unit>, IMessageBoxOkViewModel
{
    [Reactive] public string Title { get; set; } = Language.CancelDownloadOverlayView_Title;

    [Reactive]
    public string Description { get; set; } = "This is some very long design only text that spans multiple lines!! This text is super cool!!";

    [Reactive] public required IMarkdownRendererViewModel? MarkdownRenderer { get; set; }
    
    /// <summary>
    /// Shows the 'Game is already Running' error when you try to synchronize and a game is already running (usually on Windows).
    /// </summary>
    public static Task ShowGameAlreadyRunningError(IServiceProvider serviceProvider, string gameName)
    {
        return Show(serviceProvider, Language.ErrorGameAlreadyRunning_Title, string.Format(Language.ErrorGameAlreadyRunning_Description, gameName));
    }

    public static async Task Show(IServiceProvider serviceProvider, string title, string description, string? markdown = null)
    {
        var overlayController = serviceProvider.GetRequiredService<IOverlayController>();
        
        IMarkdownRendererViewModel? markdownRenderer = null;
        if (markdown != null)
        {
            markdownRenderer = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
            markdownRenderer.Contents = markdown;
        }
        
        var viewModel = new MessageBoxOkViewModel
        {
            Title = title,
            Description = description,
            MarkdownRenderer = markdownRenderer,
        };
        await overlayController.EnqueueAndWait(viewModel);
    }
}
