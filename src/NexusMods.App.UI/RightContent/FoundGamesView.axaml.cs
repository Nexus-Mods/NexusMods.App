using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.RightContent;

public partial class FoundGamesView : ReactiveUserControl<FoundGamesViewModel>
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