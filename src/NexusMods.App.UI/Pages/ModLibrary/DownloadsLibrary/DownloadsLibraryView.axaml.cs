using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary;

public partial class DownloadsLibraryView : ReactiveUserControl<IDownloadsLibraryViewModel>
{
    public DownloadsLibraryView()
    {
        InitializeComponent();
    }
}

