using Avalonia.Controls;

namespace NexusMods.App.UI.MessageBox;

public class MessageBox<TV, TVM, T> : IMessageBox<T>
    where TV : UserControl, IMessageBoxView<T>
    where TVM : IMessageBoxViewModel<T>
    where T : struct
{
    private TV _view;
    private TVM _viewModel;

    public MessageBox(TV view, TVM viewModel)
    {
        _view = view;
        _viewModel = viewModel;
    }

    /// <summary>
    ///  Show messagebox as window
    /// </summary>
    /// <returns></returns>
    public Task<T> ShowWindowAsync()
    {
        _viewModel.SetView(_view);
        
        var window = new MessageBoxWindow()
        {
            Content = _view,
            DataContext = _viewModel,
        };
        
        window.Closed += _view.CloseWindow;
        var tcs = new TaskCompletionSource<T>();

        _view.SetCloseAction(() =>
        {
            tcs.TrySetResult(_view.GetButtonResult());
            window.Close();
        });

        window.ShowInTaskbar = true;
        
        window.Show();
        return tcs.Task;
    }

    /// <summary>
    ///  Show messagebox as window with owner
    /// </summary>
    /// <param name="owner">Window owner </param>
    /// <returns></returns>
    public Task<T> ShowWindowDialogAsync(Window owner)
    {
        _viewModel.SetView(_view);
        
        var window = new MessageBoxWindow()
        {
            Content = _view,
            DataContext = _viewModel,
        };
        
        window.Closed += _view.CloseWindow;
        var tcs = new TaskCompletionSource<T>();

        _view.SetCloseAction(() =>
        {
            tcs.TrySetResult(_view.GetButtonResult());
            window.Close();
        });
        
        window.ShowDialog(owner);
        return tcs.Task;
    }

    
}
