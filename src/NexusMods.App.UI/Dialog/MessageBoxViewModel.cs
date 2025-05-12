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

    public ReactiveCommand<ButtonDefinitionId, Unit> CloseWindowCommand { get; }
    public IDialogView<ButtonDefinitionId>? View { get; set; }
    public ButtonDefinitionId Result { get; set; } 

    public IDialogContentViewModel? ContentViewModel { get; set; }
    
    public MessageBoxViewModel(
        string title, 
        string text, 
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize = MessageBoxSize.Small,
        IDialogContentViewModel? contentViewModel = null)
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

        ContentViewModel = contentViewModel;
        ContentViewModel?.SetParent(this);
        
        CloseWindowCommand = ReactiveCommand.Create<ButtonDefinitionId>(CloseWindow);
    }

    public void SetView(IDialogView<ButtonDefinitionId> view)
    {
        View = view;
    }

    public async void CloseWindow(ButtonDefinitionId id)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (View is null) return;
                
                View.SetButtonResult(id);
                View.Close();
            }
        );
    }
}
