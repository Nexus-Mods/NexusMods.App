using System.Reactive;
using Avalonia.Threading;
using ExCSS;
using NexusMods.App.UI.MessageBox.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.MessageBox;

public class MessageBoxViewModel : IMessageBoxViewModel<ButtonDefinitionId>
{
    private IMessageBoxView<ButtonDefinitionId>? _view;
    
    public MessageBoxButtonDefinition[] ButtonDefinitions { get; }
    public string ContentTitle { get; }
    public string ContentMessage { get; set; }
    public MessageBoxSize MessageBoxSize { get; }

    public ReactiveCommand<ButtonDefinitionId, Unit> ButtonClickCommand { get; }

    public MessageBoxViewModel(
        string title, 
        string text, 
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize = MessageBoxSize.Small)
    {
        ContentTitle = title;
        ContentMessage = text;
        ButtonDefinitions = buttonDefinitions;
        MessageBoxSize = messageBoxSize;

        ButtonClickCommand = ReactiveCommand.Create<ButtonDefinitionId>(ButtonClick);
    }

    public void SetView(IMessageBoxView<ButtonDefinitionId> view)
    {
        _view = view;
    }

    private async void ButtonClick(ButtonDefinitionId id)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_view is null) return;
                
                //_view.SetButtonResult(Enum.Parse<ButtonResult>(s.Trim(), true));
                _view.SetButtonResult(id);
                _view.Close();
            }
        );
    }
}
