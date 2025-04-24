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
        CanResize = false;
        // ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //SystemDecorations = SystemDecorations.Full;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        
        InitializeComponent();
    }
}

