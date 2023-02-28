using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NexusMods.App.UI.Controls.TopBar;

public partial class TopBarView : UserControl
{
    public TopBarView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}