using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using R3;
using SerialDisposable = System.Reactive.Disposables.SerialDisposable;

namespace NexusMods.App.UI.Dialog;

public partial class DialogWindow : Window, IDisposable
{
    private SerialDisposable _serialDisposable;
        
    public DialogWindow()
    {
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;

        InitializeComponent();
        
        _serialDisposable = new SerialDisposable();

        // Bind the CloseCommand to the Window's close action
        this.DataContextChanged += (sender, args) =>
        {
            if (DataContext is IDialogViewModel viewModel)
            {
                // Bind the CloseCommand to the Window's close action
                _serialDisposable.Disposable = viewModel.ButtonPressCommand.Subscribe( id => 
                {
                    Close();
                });
            }
            else
            {
                _serialDisposable.Disposable = null;
            }
        };
    }
    
    public void Dispose()
    {
        _serialDisposable.Dispose();
    }
}
