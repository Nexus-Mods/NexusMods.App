using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public partial class CollectionCardView : ReactiveUserControl<ICollectionCardViewModel>
{
    public CollectionCardView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Name, view => view.TitleText.Text)
                .DisposeWith(d);
            
            this.WhenAnyValue(view => view.ViewModel!.Name)
                .SubscribeWithErrorLogging(name =>
                {
                    TitleText.Text = name;
                    ToolTip.SetTip(TitleText, name);
                })
                .DisposeWith(d);
            

            this.OneWayBind(ViewModel, vm => vm.Image, view => view.TileImage.Source)
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.RevisionNumber, view => view.RevisionText.Text, revision => $"Revision {revision}")
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.NumDownloads, view => view.ModsCountText.Text, count => $"{count:N0} Mods")
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TotalSize, view => view.TotalSize.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenCollectionDownloadPageCommand, view => view.DownloadButton)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.IsCollectionInstalled)
                .SubscribeWithErrorLogging(isInstalled =>
                {
                    DownloadButton.Text = isInstalled ? "Installed" : "Download";
                    DownloadButton.Type = isInstalled ? StandardButton.Types.Tertiary  : StandardButton.Types.Secondary;
                    DownloadButton.Fill = isInstalled ? StandardButton.Fills.Weak  : StandardButton.Fills.Strong;
                    DownloadButton.ShowIcon = isInstalled ? StandardButton.ShowIconOptions.Left : StandardButton.ShowIconOptions.Both;
                    DownloadButton.LeftIcon = isInstalled ? IconValues.Check : IconValues.Download;
                    DownloadButton.RightIcon = isInstalled ? null : IconValues.ChevronRight;
                })
                .DisposeWith(d);
        });
    }
    
}

