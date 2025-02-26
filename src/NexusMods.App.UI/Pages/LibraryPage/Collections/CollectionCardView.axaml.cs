using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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

            this.OneWayBind(ViewModel, vm => vm.Image, view => view.TileImage.Source)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Category, view => view.CategoryText.Text)
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.RevisionNumber, view => view.RevisionNumberText.Text, revision => $"Revision {revision}")
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.Summary, view => view.SummaryText.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.NumDownloads, view => view.NumDownloads.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.EndorsementCount, view => view.Endorsements.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TotalDownloads, view => view.TotalDownloads.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TotalSize, view => view.TotalSize.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.OverallRating, view => view.OverallRating.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsAdult, view => view.AdultStackPanel.IsVisible)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorAvatar, view => view.AuthorAvatarImage.Source)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenCollectionDownloadPageCommand, view => view.DownloadButton)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.OverallRating)
                .Select(rating =>
                {
                    return rating.Value switch
                    {
                        >= 0.75 => "HighRating",
                        >= 0.5 => "MidRating",
                        >= 0.01 => "LowRating",
                        _ => "NoRating",
                    };
                })
                .Subscribe(className =>
                    {
                        OverallRatingPanel.Classes.Add(className);
                        OverallRating.Text = className == "NoRating" ? "--" : ViewModel!.OverallRating.Value.ToString("P2");
                    }
                )
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.IsCollectionInstalled)
                .SubscribeWithErrorLogging(isInstalled =>
                {
                    DownloadButton.Text = isInstalled ? "Installed" : "Continue Download";
                    DownloadButton.LeftIcon = isInstalled ? IconValues.Check : IconValues.Download;
                    DownloadButton.RightIcon = isInstalled ? null : IconValues.ChevronRight;
                    DownloadButton.ShowIcon = isInstalled ? StandardButton.ShowIconOptions.Left : StandardButton.ShowIconOptions.Both;
                })
                .DisposeWith(d);
        });
    }
}

