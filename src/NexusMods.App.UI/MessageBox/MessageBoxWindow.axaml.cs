using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace NexusMods.App.UI.MessageBox;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow()
    {
        CanResize = false;
        ShowInTaskbar = false;
        MinWidth = 280;
        MaxWidth = 500;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //SystemDecorations = SystemDecorations.Full;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        InitializeComponent();
    }
}

