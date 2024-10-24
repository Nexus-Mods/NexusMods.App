using NexusMods.App.UI.Resources;
using R3;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;

public class MessageBoxOkViewModel : AOverlayViewModel<IMessageBoxOkViewModel, Unit>, IMessageBoxOkViewModel
{
    [Reactive] public string Title { get; set; } = Language.CancelDownloadOverlayView_Title;

    [Reactive]
    public string Description { get; set; } = "This is some very long design only text that spans multiple lines!! This text is super cool!!";

    /// <summary>
    /// Shows the 'Game is already Running' error when you try to synchronize and a game is already running (usually on Windows).
    /// </summary>
    public static async Task ShowGameAlreadyRunningError(IOverlayController overlayController)
    {
        var viewModel = new MessageBoxOkViewModel()
        {
            Title = Language.ErrorGameAlreadyRunning_Title,
            Description = Language.ErrorGameAlreadyRunning_Description,
        };
        await overlayController.EnqueueAndWait(viewModel);
    }
}
