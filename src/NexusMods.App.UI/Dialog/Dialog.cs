using Avalonia;
using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public class Dialog : IDialog
{
    private DialogViewModel _viewModel;
    private bool _hasUserResized = false;

    public Dialog(DialogViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <summary>
    /// Displays the dialog window.
    /// </summary>
    /// <param name="owner">The parent window that owns this dialog.</param>
    /// <param name="isModal">Indicates whether the dialog should be modal.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the dialog.</returns>
    public Task<StandardDialogResult> Show(Window owner, bool isModal = false)
    {
        var window = new DialogWindow()
        {
            DataContext = _viewModel,

            Title = _viewModel.WindowTitle,
            Width = _viewModel.DialogWindowSize switch
            {
                DialogWindowSize.Small => 400,
                DialogWindowSize.Medium => 600,
                DialogWindowSize.Large => 800
            },
            MaxHeight = _viewModel.DialogWindowSize switch
            {
                DialogWindowSize.Small => 320,
                DialogWindowSize.Medium => 500,
                DialogWindowSize.Large => 600
            },
            CanResize = true,
            SizeToContent = SizeToContent.Height, // Height is set by Avalonia based on content, we set the width above
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = !isModal, // Show the window in the taskbar if it's not modal

            MinWidth = 240,
            MinHeight = 150
        };

        var tcs = new TaskCompletionSource<StandardDialogResult>();

        // when the window is closed, set the result and complete the task
        window.Closed += (o, args) =>
        {
            window.Dispose();
            tcs.TrySetResult(_viewModel.Result);
        };

        // show the window in the taskbar if it's not modal
        if (isModal)
            window.ShowDialog(owner);
        else
            window.Show();

        return tcs.Task;
    }
}
