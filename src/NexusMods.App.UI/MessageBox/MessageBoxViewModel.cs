using System.Reactive;
using Avalonia.Threading;
using ExCSS;
using NexusMods.App.UI.MessageBox.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.MessageBox;

public class MessageBoxViewModel : IMessageBoxViewModel<ButtonResult>
{
    public readonly ClickEnum _defaultEnterButton;
    public readonly ClickEnum _defaultEscapeButton;
    private IMessageBoxView<ButtonResult>? _view;

    public string ContentTitle { get; }
    public string ContentMessage { get; set; }

    public bool IsOkShowed { get; private set; }
    public bool IsYesShowed { get; private set; }
    public bool IsNoShowed { get; private set; }
    public bool IsAbortShowed { get; private set; }
    public bool IsCancelShowed { get; private set; }

    public ReactiveCommand<string, Unit> ButtonClickCommand { get; }
    public ReactiveCommand<Unit, Unit> EnterClickCommand { get; }
    public ReactiveCommand<Unit, Unit> EscClickCommand { get; }

    public MessageBoxViewModel(string title, string text, ButtonEnum buttonEnum)
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
        switch (_defaultEnterButton)
        {
            case ClickEnum.Ok:
                ButtonClick(ButtonResult.Ok);
                return;
            case ClickEnum.Yes:
                ButtonClick(ButtonResult.Yes);
                return;
            case ClickEnum.No:
                ButtonClick(ButtonResult.No);
                return;
            case ClickEnum.Abort:
                ButtonClick(ButtonResult.Abort);
                return;
            case ClickEnum.Cancel:
                ButtonClick(ButtonResult.Cancel);
                return;
            case ClickEnum.None:
                ButtonClick(ButtonResult.None);
                return;
            case ClickEnum.Default:
            {
                if (IsOkShowed)
                {
                    ButtonClick(ButtonResult.Ok);
                    return;
                }
                if (IsYesShowed)
                {
                    ButtonClick(ButtonResult.Yes);
                    return;
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void EscClick()
    {
        switch (_defaultEscapeButton)
        {
            case ClickEnum.Ok:
                ButtonClick(ButtonResult.Ok);
                return;
            case ClickEnum.Yes:
                ButtonClick(ButtonResult.Yes);
                return;
            case ClickEnum.No:
                ButtonClick(ButtonResult.No);
                return;
            case ClickEnum.Abort:
                ButtonClick(ButtonResult.Abort);
                return;
            case ClickEnum.Cancel:
                ButtonClick(ButtonResult.Cancel);
                return;
            case ClickEnum.None:
                ButtonClick(ButtonResult.None);
                return;
            case ClickEnum.Default:
            {
                if (IsCancelShowed)
                {
                    ButtonClick(ButtonResult.Cancel);
                    return;
                }
                if (IsAbortShowed)
                {
                    ButtonClick(ButtonResult.Abort);
                    return;
                }
                if (IsNoShowed)
                {
                    ButtonClick(ButtonResult.No);
                    return;
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ButtonClick(ButtonResult.None);
    }

    public void SetView(IMessageBoxView<ButtonResult> view)
    {
        _view = view;
    }

    private async void ButtonClick(string s)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_view is null) return;
                _view.SetButtonResult(Enum.Parse<ButtonResult>(s.Trim(), true));
                _view.Close();
            }
        );
    }

    private async void ButtonClick(ButtonResult buttonResult)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_view is null) return;
                _view.SetButtonResult(buttonResult);
                _view.Close();
            }
        );
    }

    private void SetButtons(ButtonEnum buttonEnum)
    {
        switch (buttonEnum)
        {
            case ButtonEnum.Ok:
                IsOkShowed = true;
                break;
            case ButtonEnum.YesNo:
                IsYesShowed = true;
                IsNoShowed = true;
                break;
            case ButtonEnum.OkCancel:
                IsOkShowed = true;
                IsCancelShowed = true;
                break;
            case ButtonEnum.OkAbort:
                IsOkShowed = true;
                IsAbortShowed = true;
                break;
            case ButtonEnum.YesNoCancel:
                IsYesShowed = true;
                IsNoShowed = true;
                IsCancelShowed = true;
                break;
            case ButtonEnum.YesNoAbort:
                IsYesShowed = true;
                IsNoShowed = true;
                IsAbortShowed = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(buttonEnum), buttonEnum, null);
        }
    }
}
