using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NexusMods.App.UI.Controls.Buttons;

public partial class Scratch : UserControl
{
    public Scratch()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
