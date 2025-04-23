using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;

namespace NexusMods.App.UI.Dialog;

public partial class DialogContainerView : UserControl, IDialogView<ButtonDefinitionId>
{
    private ButtonDefinitionId _buttonResult = ButtonDefinitionId.From("none");
    private Action? _closeAction;
    
    private Button? closeButton;
    
    public DialogContainerView()
    {
        InitializeComponent();
        
        closeButton = this.FindControl<Button>("CloseButton");
        
        if(closeButton != null)
            closeButton.Click += CloseWindow;
    }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        // need this here so DataContext is set
    }

    

    public void CloseWindow(object? sender, EventArgs eventArgs)
    {
        this.Close();
    }

    public void SetCloseAction(Action closeAction)
    {
        _closeAction = closeAction;
    }

    public void SetButtonResult(ButtonDefinitionId buttonResult)
    {
        _buttonResult = buttonResult;
    }

    public ButtonDefinitionId GetButtonResult()
    {
        return _buttonResult;
    }

    public void Close()
    {
        _closeAction?.Invoke();
    }
}

