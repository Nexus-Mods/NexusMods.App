using Avalonia.Controls;

namespace NexusMods.UI.Theme;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}