using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public partial class DownloadsView : UserControl
{
    public DownloadsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

