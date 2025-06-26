using Avalonia;
using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public class Dialog<TView, TViewModel> : IDialog
    where TView : UserControl, IDialogView
    where TViewModel : IDialogViewModel
{
    private TView _view;
    private TViewModel _viewModel;
    private bool _hasUserResized = false;

    public Dialog(TView view, TViewModel viewModel)
    {
        _view = view;
        _viewModel = viewModel;
    }

    public Task<ButtonDefinitionId> ShowWindow(Window owner, bool isModal = false)
    {
        // Get the initial size and position of the owner window
        var ownerSize = owner.ClientSize;
        var ownerPosition = owner.Position;

        var window = new DialogWindow()
        {
            Content = _view,
            DataContext = _viewModel,

            Title = _viewModel.WindowTitle,
            Width = _viewModel.DialogWindowSize switch
            {
                DialogWindowSize.Small => 400,
                DialogWindowSize.Medium => 600,
                DialogWindowSize.Large => 800,
                _ => 600
            },
            CanResize = true,
            SizeToContent = SizeToContent.Height, // Height is set by Avalonia based on content
            MaxHeight = ownerSize.Height * 0.8, // We don't ever want the auto height sizing to be greater than 80% of the owner window height
            WindowStartupLocation = WindowStartupLocation.Manual, // we position the window ourselves in the Resized event
            ShowInTaskbar = !isModal, // Show the window in the taskbar if it's not modal
        };

        var tcs = new TaskCompletionSource<ButtonDefinitionId>();

        // when the window is closed, set the result and complete the task
        window.Closed += (o, args) =>
        {
            window.Dispose();
            tcs.TrySetResult(_viewModel.Result);
        };

        window.Resized += (o, args) =>
        {
            //Console.WriteLine($@"{args.Reason} {args.ClientSize} _hasUserResized={_hasUserResized}");

            // If the window is resized by the user, turn off any MaxHeight and tell the window not to autosize window to content anymore
            // Set the flag so that the window doesn't get manually positioned by us and doesn't run this code each frame
            if (args.Reason == WindowResizeReason.User && !_hasUserResized)
            {
                window.MaxHeight = double.PositiveInfinity;
                window.SizeToContent = SizeToContent.Manual;
                _hasUserResized = true;
                return;
            }
            
            // If the window has already been resized by the user, we are done here
            if (_hasUserResized)
                return;

            // Set the position to the center of the owner but only if the window hasn't been resized manually.
            // This feels a bit hacky as Avalonia does multiple resizes during opening, especially when we are using
            // ViewModelViewHosts and it trying to auto size the window to the content
            window.Position = new PixelPoint(
                (int)(ownerPosition.X + (ownerSize.Width / 2) - (window.Width / 2)),
                (int)(ownerPosition.Y + (ownerSize.Height / 2) - (window.Height / 2))
            );
        };

        // show the window in the taskbar if it's not modal
        if (isModal)
            window.ShowDialog(owner);
        else
            window.Show();

        return tcs.Task;
    }
}
