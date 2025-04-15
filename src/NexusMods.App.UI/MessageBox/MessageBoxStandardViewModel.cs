using System.Reactive;
using Avalonia.Threading;
using ExCSS;
using NexusMods.App.UI.MessageBox.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.MessageBox;

public class MessageBoxStandardViewModel : IMessageBoxViewModel<ButtonResult>
{
    public string ContentTitle { get; }
    public string ContentMessage { get; set; }
    public bool IsOkShowed { get; private set; }
    public bool IsCancelShowed { get; private set; }

    public ReactiveCommand<string, Unit> ButtonClickCommand { get; }
    public ReactiveCommand<Unit, Unit> EnterClickCommand { get; }
    public ReactiveCommand<Unit, Unit> EscClickCommand { get; }

    public MessageBoxStandardViewModel(string title, string text, ButtonEnum buttonEnum)
    {
        ContentTitle = title;
        ContentMessage = text;
        SetButtons(buttonEnum);

        ButtonClickCommand = ReactiveCommand.Create<string>(ButtonClick);
        EnterClickCommand = ReactiveCommand.Create(EnterClick);
        EscClickCommand = ReactiveCommand.Create(EscClick);
    }


    private void EnterClick()
    {
        
    }
    
    private void EscClick()
    {
        
    }
    
    private void ButtonClick(string s)
    {
        
    }

    private void SetButtons(ButtonEnum buttonEnum)
    {
        switch (buttonEnum)
        {
            case ButtonEnum.Ok:
                IsOkShowed = true;
                break;
            case ButtonEnum.OkCancel:
                IsOkShowed = true;
                IsCancelShowed = true;
                break;
            case ButtonEnum.YesNo:
            case ButtonEnum.OkAbort:
            case ButtonEnum.YesNoCancel:
            case ButtonEnum.YesNoAbort:
            default:
                throw new ArgumentOutOfRangeException(nameof(buttonEnum), buttonEnum, null);
        }
    }
}
