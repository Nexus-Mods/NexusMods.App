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
    
    public Task<TResult> ShowWindow(Window? owner = null, bool isModal = false)
    {
        _viewModel.SetView(_view);

        var window = new DialogWindow()
        {
            Content = _view,
            DataContext = _viewModel,
            
            Title = _viewModel.WindowTitle,

            MaxWidth = _viewModel.WindowMaxWidth
        };

        window.Closed += _view.CloseWindow;
        var tcs = new TaskCompletionSource<TResult>();
        
        _view.SetCloseAction(() =>
        {
            tcs.TrySetResult(_view.GetButtonResult());
            window.Close();
        });

        if (isModal && owner != null)
        {
            window.ShowInTaskbar = false;
            window.ShowDialog(owner);
        }
        else
        {
            window.ShowInTaskbar = true;
            window.Show();
        }

        return tcs.Task;
    }
}
