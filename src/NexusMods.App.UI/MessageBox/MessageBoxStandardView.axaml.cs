using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

public partial class MessageBoxStandardView : UserControl
{
    private ButtonResult _buttonResult = ButtonResult.None;
    
    public MessageBoxStandardView()
    {
        InitializeComponent();
    }
    
    public void SetButtonResult(ButtonResult bdName)
    {
        _buttonResult = bdName;
    }

    public ButtonResult GetButtonResult()
    {
        return _buttonResult;
    }

    public void Close()
    {
        //_closeAction?.Invoke();
    }

    public void CloseWindow(object sender, EventArgs eventArgs)
    {
        Close();
    }
}

