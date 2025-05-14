using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public class Dialog<TView, TViewModel, TResult> : IDialog<TResult>
    where TView : UserControl, IDialogView<TResult>
    where TViewModel : IDialogViewModel<TResult>
{
    private TView _view;
    private TViewModel _viewModel;

    public Dialog(TView view, TViewModel viewModel)
    {
        _view = view;
        _viewModel = viewModel;
    }

    public Task<TResult?> ShowWindow(Window owner, bool isModal = false)
    {
        var window = new DialogWindow()
        {
            Content = _view,
            DataContext = _viewModel,
            
            Title = _viewModel.WindowTitle,
            Width = _viewModel.WindowWidth,
            // never want to be taller than the owner window
            MaxHeight = owner.Height - 50,
            CanResize = true,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = !isModal
        };

        var tcs = new TaskCompletionSource<TResult?>();
        
        // when the window is closed, set the result and complete the task
        window.Closed += (o, args) =>
        {
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
