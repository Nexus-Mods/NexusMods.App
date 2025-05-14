using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace NexusMods.App.UI.Dialog;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;

        InitializeComponent();

        // Bind the CloseCommand to the Window's close action
        this.DataContextChanged += (sender, args) =>
        {
            if (DataContext is MessageBoxViewModel viewModel)
            {
                // when the close button is clicked, close the window
                viewModel.CloseWindowCommand.Subscribe(result =>
                    {
                        Close();
                    }
                );
            }
        };
    }
}
