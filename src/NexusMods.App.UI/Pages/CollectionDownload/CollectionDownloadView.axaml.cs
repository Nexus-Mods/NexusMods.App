using System.Reactive.Disposables;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public partial class CollectionDownloadView : ReactiveUserControl<ICollectionDownloadViewModel>
{
    public CollectionDownloadView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionDownloadView, ICollectionDownloadViewModel, LibraryItemModel, EntityId>(this, RequiredModsTreeDataGrid,
            vm => vm.RequiredModsAdapter
        );
        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionDownloadView, ICollectionDownloadViewModel, LibraryItemModel, EntityId>(this, OptionalModsTreeDataGrid,
            vm => vm.OptionalModsAdapter
        );

        this.WhenActivated(d =>
            {
                
                // Uncomment this to enable the background image
                 this.WhenAnyValue(view => view.ViewModel!.BackgroundImage)
                     .WhereNotNull()
                     .SubscribeWithErrorLogging(image => Body.Background = new ImageBrush { Source = image, Stretch = Stretch.UniformToFill})
                     .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.TileImage)
                    .WhereNotNull()
                    .SubscribeWithErrorLogging(image => CollectionImage.Source = image)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.Name, view => view.Heading.Text)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Summary, view => view.Summary.Text)
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
                
                this.OneWayBind(ViewModel, vm => vm.RequiredModCount, view => view.RequiredModsCount.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.OptionalModCount, view => view.OptionalModsCount.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.CollectionStatusText, view => view.CollectionStatusText.Text)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.RequiredModsAdapter.Source.Value, view => view.RequiredModsTreeDataGrid.Source)
                    .AddTo(d);
                
                this.OneWayBind(ViewModel, vm => vm.OptionalModsAdapter.Source.Value, view => view.OptionalModsTreeDataGrid.Source)
                    .AddTo(d);

            }
        );
    }
    
    
}

