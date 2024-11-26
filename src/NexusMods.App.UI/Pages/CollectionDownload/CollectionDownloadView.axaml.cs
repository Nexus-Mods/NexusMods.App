using System.Diagnostics;
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

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionDownloadView, ICollectionDownloadViewModel, ILibraryItemModel, EntityId>(this, RequiredDownloadsTree, vm => vm.TreeDataGridAdapter);
        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionDownloadView, ICollectionDownloadViewModel, ILibraryItemModel, EntityId>(this, OptionalDownloadsTree, vm => vm.TreeDataGridAdapter);

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.DownloadAllCommand, view => view.DownloadAllButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TreeDataGridAdapter.Source.Value, view => view.RequiredDownloadsTree.Source)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TreeDataGridAdapter.Source.Value, view => view.OptionalDownloadsTree.Source)
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

            this.WhenAnyValue(view => view.TabControl.SelectedItem)
                .Select(selectedItem =>
                {
                    if (ReferenceEquals(selectedItem, RequiredTab)) return CollectionDownloadsFilter.OnlyRequired;
                    if (ReferenceEquals(selectedItem, OptionalTab)) return CollectionDownloadsFilter.OnlyOptional;
                    throw new UnreachableException();
                })
                .Subscribe(filter =>
                {
                    ViewModel!.TreeDataGridAdapter.Filter.Value = filter;
                })
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Select(static vm => vm.OptionalDownloadsCount > 0)
                .Subscribe(hasOptionalDownloads =>
                {
                    if (hasOptionalDownloads) TabControl.Classes.Remove("SingleTab");
                    else TabControl.Classes.Add("SingleTab");

                    if (!hasOptionalDownloads) TabControl.SelectedItem = RequiredTab;
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

