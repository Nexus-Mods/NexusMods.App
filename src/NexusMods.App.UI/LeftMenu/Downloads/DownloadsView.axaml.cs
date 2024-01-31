using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public partial class DownloadsView : ReactiveUserControl<IDownloadsViewModel>
{
    public DownloadsView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Items)
                .BindTo(this, x => x.MenuItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}

