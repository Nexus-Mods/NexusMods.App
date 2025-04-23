using Avalonia.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public class Dialog<TView, TViewModel, TReturn> : IDialog<TReturn>
    where TView : UserControl, IMessageBoxView<TReturn>
    where TViewModel : IDialogViewModel<TReturn>
{
    private TView _view;
    private TViewModel _viewModel;

    public Dialog(TView view, TViewModel viewModel)
    {
        _view = view;
        _viewModel = viewModel;
    }
    
    public Task<TReturn> ShowWindow(Window? owner = null, bool isModal = false)
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
        var tcs = new TaskCompletionSource<TReturn>();
        
        _view.SetCloseAction(() =>
        {
            tcs.TrySetResult(_view.GetButtonResult());
            window.Close();
        });

        if (isModal && owner != null)
        {
            window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return tcs.Task;
    }
}
