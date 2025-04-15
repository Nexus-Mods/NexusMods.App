using Avalonia.Controls;

namespace NexusMods.App.UI.MessageBox;

public class MessageBox<V, VM, T> : IMessageBox<T>
    where V : UserControl
    where VM : IMessageBoxViewModel<T>
    where T : struct
{
    private readonly V _view;
    private readonly VM _viewModel;

    public MessageBox(V view, VM viewModel)
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
        var window = new MessageBoxWindow()
        {
            Content = _view,
            DataContext = _viewModel,
        };
        
        var tcs = new TaskCompletionSource<T>();

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
        var window = new MessageBoxWindow()
        {
            Content = _view,
            DataContext = _viewModel,
        };
        
        var tcs = new TaskCompletionSource<T>();
        
        window.ShowDialog(owner);
        return tcs.Task;
    }
}
