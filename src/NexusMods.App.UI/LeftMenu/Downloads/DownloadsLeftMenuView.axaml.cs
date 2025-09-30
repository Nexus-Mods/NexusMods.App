using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

[UsedImplicitly]
public partial class DownloadsLeftMenuView : ReactiveUserControl<IDownloadsLeftMenuViewModel>
{
    public DownloadsLeftMenuView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemAllDownloads, view => view.AllDownloadsItem.ViewModel)
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemsPerGameDownloads, view => view.PerGameDownloadsItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}
