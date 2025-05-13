using System.ComponentModel;
using System.Reactive;
using Avalonia.Threading;
using ExCSS;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class MessageBoxViewModel : IDialogViewModel<ButtonDefinitionId>
{
    public MessageBoxButtonDefinition[] ButtonDefinitions { get; }
    public ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> CloseWindowCommand { get; }
    public string WindowTitle { get; }
    public double WindowWidth { get; }
    public string ContentMessage { get; set; }
    public IconValue? Icon { get; }
    public MessageBoxSize MessageBoxSize { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;

    public IDialogView<ButtonDefinitionId>? View { get; set; }
    public ButtonDefinitionId Result { get; set; }

    public IDialogContentViewModel? ContentViewModel { get; set; }

    public MessageBoxViewModel(
        string title,
        string text,
        IconValue? icon,
        MessageBoxButtonDefinition[] buttonDefinitions,
        MessageBoxSize messageBoxSize = MessageBoxSize.Small,
        IDialogContentViewModel? contentViewModel = null)
    {
        WindowTitle = title;
        ContentMessage = text;
        ButtonDefinitions = buttonDefinitions;
        MessageBoxSize = messageBoxSize;
        WindowWidth = messageBoxSize switch
        {
            MessageBoxSize.Small => 320,
            MessageBoxSize.Medium => 480,
            MessageBoxSize.Large => 640,
            _ => 320,
        };

        Icon = icon;
        ContentViewModel = contentViewModel;

        CloseWindowCommand = ReactiveCommand.Create<ButtonDefinitionId, ButtonDefinitionId>((id) =>
            {
                Result = id;
                return id;
            }
        );
    }
}
