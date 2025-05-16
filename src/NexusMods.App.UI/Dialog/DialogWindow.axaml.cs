using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

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
            if (DataContext is DialogViewModel viewModel)
            {
                // when the close button is clicked, close the window
                _serialDisposable.Disposable = viewModel.CloseWindowCommand.Subscribe(result =>
                    {
                        Close();
                    }
                );
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
