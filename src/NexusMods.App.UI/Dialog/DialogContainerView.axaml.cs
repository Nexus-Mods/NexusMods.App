using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public partial class DialogContainerView : ReactiveUserControl<DialogContainerViewModel>, IDialogView<ButtonDefinitionId>
{
    private ButtonDefinitionId _buttonResult = ButtonDefinitionId.From("none");
    private Action? _closeAction;
    private Button? closeButton;

    public DialogContainerView()
    {
        InitializeComponent();

        closeButton = this.FindControl<Button>("CloseButton");

        if (closeButton != null)
            closeButton.Click += CloseWindow;

        // Set the ViewModel for the ViewModelViewHost in the code-behind
        this.WhenActivated(disposables =>
        {
            // Bind the title text block to the ViewModel's WindowTitle property.
            this.OneWayBind(ViewModel, 
                    vm => vm.WindowTitle,
                    view => view.TitleTextBlock.Text)
                .DisposeWith(disposables);
            
            // 
            this.OneWayBind(ViewModel, 
                    vm => vm.ShowWindowTitlebar,
                    view => view.Titlebar.IsVisible)
                .DisposeWith(disposables);
            
            // Bind the content view model.
            this.OneWayBind(ViewModel, 
                    vm => vm.ContentViewModel,
                    view => view.ViewModelHost.ViewModel)
                .DisposeWith(disposables);
        });
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
