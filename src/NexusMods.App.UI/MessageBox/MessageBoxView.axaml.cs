using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

public partial class MessageBoxView : UserControl, IMessageBoxView<ButtonResult>
{
    private ButtonResult _buttonResult = ButtonResult.None;
    private Action? _closeAction;
    
    private Button? closeButton;
    
    public MessageBoxView()
    {
        InitializeComponent();
        
        closeButton = this.FindControl<Button>("CloseButton");
        
        if(closeButton != null)
            closeButton.Click += CloseWindow;
    }
    

    public void CloseWindow(object? sender, EventArgs eventArgs)
    {
        this.Close();
    }

    public void SetCloseAction(Action closeAction)
    {
        _closeAction = closeAction;
    }

    public void SetButtonResult(ButtonResult buttonResult)
    {
        _buttonResult = buttonResult;
    }

    public ButtonResult GetButtonResult()
    {
        return _buttonResult;
    }

    public void Close()
    {
        _closeAction?.Invoke();
    }
}

