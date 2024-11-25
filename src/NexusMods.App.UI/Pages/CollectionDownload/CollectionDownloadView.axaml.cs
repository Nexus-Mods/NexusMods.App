using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public partial class CollectionDownloadView : ReactiveUserControl<ICollectionDownloadViewModel>
{
    public CollectionDownloadView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionDownloadView, ICollectionDownloadViewModel, ILibraryItemModel, EntityId>(this, RequiredDownloadsTree, vm => vm.RequiredDownloadsAdapter);
        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionDownloadView, ICollectionDownloadViewModel, ILibraryItemModel, EntityId>(this, OptionalDownloadsTree, vm => vm.OptionalDownloadsAdapter);

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.RequiredDownloadsAdapter.Source.Value, view => view.RequiredDownloadsTree.Source)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.OptionalDownloadsAdapter.Source.Value, view => view.OptionalDownloadsTree.Source)
                .DisposeWith(d);

             this.WhenAnyValue(view => view.ViewModel!.BackgroundImage)
                 .WhereNotNull()
                 .SubscribeWithErrorLogging(image => HeaderBorderBackground.Background = new ImageBrush { Source = image, Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Top})
                 .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.TileImage)
                .WhereNotNull()
                .SubscribeWithErrorLogging(image => CollectionImage.Source = image)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Name, view => view.Heading.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorAvatar, view => view.AuthorAvatar.Source)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Summary, view => view.Summary.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.DownloadCount, view => view.NumDownloads.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.EndorsementCount, view => view.Endorsements.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TotalDownloads, view => view.TotalDownloads.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TotalSize, view => view.TotalSize.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.OverallRating, view => view.OverallRating.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.RequiredDownloadsCount, view => view.RequiredDownloadsCount.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.OptionalDownloadsCount, view => view.OptionalDownloadsCount.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.CollectionStatusText, view => view.CollectionStatusText.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.RevisionNumber, view => view.Revision.Text, revision => $"Revision {revision}")
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(vm =>
                {
                    if (vm.OptionalDownloadsCount == 0)
                    {
                        // TabControl.IsVisible = false;
                    }
                }).DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.OverallRating)
                .Select(rating =>
                {
                    return rating.Value switch
                    {
                        >= 0.75 => "HighRating",
                        >= 0.5 => "MidRating",
                        _ => "LowRating",
                    };
                })
                .Subscribe(className => OverallRatingPanel.Classes.Add(className))
                .DisposeWith(d);
        });
    }
}

