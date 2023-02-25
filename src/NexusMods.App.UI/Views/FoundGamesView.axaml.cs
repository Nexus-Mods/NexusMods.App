using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NexusMods.App.UI.Views;

public partial class FoundGamesView : UserControl
{
    public FoundGamesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}