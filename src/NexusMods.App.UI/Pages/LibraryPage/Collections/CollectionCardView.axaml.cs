using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
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

            this.OneWayBind(ViewModel, vm => vm.Image, view => view.TileImage.Source)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Category, view => view.CategoryText.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Summary, view => view.SummaryText.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.ModCount, view => view.ModCount.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.EndorsementCount, view => view.Endorsements.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.DownloadCount, view => view.Downloads.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TotalSize, view => view.TotalSize.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.OverallRating, view => view.OverallRating.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorAvatar, view => view.AuthorAvatarImage.Source)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenCollectionDownloadPageCommand, view => view.DownloadButton)
                .DisposeWith(d);
        });
    }
}

