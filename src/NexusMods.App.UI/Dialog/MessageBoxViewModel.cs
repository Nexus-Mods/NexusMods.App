using System.ComponentModel;
using System.Reactive;
using Avalonia.Threading;
using ExCSS;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class MessageBoxViewModel : IDialogViewModel<ButtonDefinitionId>
{
    public MessageBoxButtonDefinition[] ButtonDefinitions { get; }
    public string WindowTitle { get; }
    public double WindowMaxWidth { get; }
    public bool ShowWindowTitlebar { get; } = true;
    public string ContentMessage { get; set; }
    public MessageBoxSize MessageBoxSize { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;
    
    public IDialogView<ButtonDefinitionId>? View { get; set; }
    public ButtonDefinitionId Return { get; set; }
    public ReactiveCommand<ButtonDefinitionId, Unit> ButtonClickCommand { get; }

    public MessageBoxViewModel(
        string title, 
        string text, 
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize = MessageBoxSize.Small)
    {
        WindowTitle = title;
        ContentMessage = text;
        ButtonDefinitions = buttonDefinitions;
        MessageBoxSize = messageBoxSize;
        WindowMaxWidth = messageBoxSize switch
        {
            MessageBoxSize.Small => 320,
            MessageBoxSize.Medium => 480,
            MessageBoxSize.Large => 640,
            _ => 320
        };
        
        ButtonClickCommand = ReactiveCommand.Create<ButtonDefinitionId>(ButtonClick);
    }

    public void SetView(IDialogView<ButtonDefinitionId> view)
    {
        View = view;
    }

    private async void ButtonClick(ButtonDefinitionId id)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (View is null) return;
                
                //_view.SetButtonResult(Enum.Parse<ButtonResult>(s.Trim(), true));
                View.SetButtonResult(id);
                View.Close();
            }
        );
    }

}
