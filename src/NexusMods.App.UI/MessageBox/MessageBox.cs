using Avalonia.Controls;
using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

public class MessageBox<TView, TViewModel, T> : IMessageBox<T>
    where TView : UserControl, IMessageBoxView<T>
    where TViewModel : IMessageBoxViewModel<T>
{
    private TView _view;
    private TViewModel _viewModel;

    public MessageBox(TView view, TViewModel viewModel)
    {
        _view = view;
        _viewModel = viewModel;
    }
    
    public static MessageBox<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId> Create(
        string title, 
        string text, 
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize)
    {
        var viewModel = new MessageBoxViewModel(title, text, buttonDefinitions, messageBoxSize);
        var view = new MessageBoxView()
        {
            DataContext = viewModel,
        };

        return new MessageBox<MessageBoxView, MessageBoxViewModel, ButtonDefinitionId>(view, viewModel);
    }
    
    public Task<T> ShowWindow(Window? owner = null, bool isDialog = false)
    {
        _viewModel.SetView(_view);

        var window = new MessageBoxWindow()
        {
            Content = _view,
            DataContext = _viewModel,

            MaxWidth = _viewModel.MessageBoxSize switch
            {
                MessageBoxSize.Small => 320,
                MessageBoxSize.Medium => 480,
                MessageBoxSize.Large => 640,
                _ => 320
            },
        };

        window.Closed += _view.CloseWindow;
        var tcs = new TaskCompletionSource<T>();
        
        _view.SetCloseAction(() =>
        {
            tcs.TrySetResult(_view.GetButtonResult());
            window.Close();
        });

        if (isDialog && owner != null)
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
