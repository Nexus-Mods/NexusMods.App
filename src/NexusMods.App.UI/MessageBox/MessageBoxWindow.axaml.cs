using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NexusMods.App.UI.MessageBox;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow()
    {
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        MinWidth = 280;
        MinHeight = 123;
        MaxWidth = 500;
        MaxHeight = 800;
        SizeToContent = SizeToContent.WidthAndHeight;
        SystemDecorations = SystemDecorations.Full;
        InitializeComponent();
    }
}

