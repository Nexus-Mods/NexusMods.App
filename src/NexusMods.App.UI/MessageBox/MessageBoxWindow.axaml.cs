using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace NexusMods.App.UI.MessageBox;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow()
    {
        CanResize = false;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //SystemDecorations = SystemDecorations.Full;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        
        InitializeComponent();
        
        // Calculate MinWidth based on button widths
        // var buttonsFlexPanel = this.FindControl<FlexPanel>("ButtonsFlexPanel");
        // if (buttonsFlexPanel == null) return;
        //
        // var totalButtonWidth = buttonsFlexPanel.Children
        //     .OfType<Button>()
        //     .Sum(button => button.MinWidth + button.Margin.Left + button.Margin.Right);
        //
        // MinWidth = totalButtonWidth + 32; // Add padding for the window
    }
}

