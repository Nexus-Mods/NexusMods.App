using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NexusMods.UI.Theme.Sandbox;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }
}