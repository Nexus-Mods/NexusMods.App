using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.RightContent;

public partial class PlaceholderView : ReactiveUserControl<IPlaceholderViewModel>
{
    public PlaceholderView()
    {
        InitializeComponent();
    }
}

