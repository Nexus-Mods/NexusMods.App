using Avalonia.ReactiveUI;
using NexusMods.App.UI.ViewModels;

namespace NexusMods.App.UI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}