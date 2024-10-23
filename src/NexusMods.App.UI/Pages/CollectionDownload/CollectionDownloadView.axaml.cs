using System.Reactive.Disposables;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public partial class CollectionDownloadView : ReactiveUserControl<ICollectionDownloadViewModel>
{
    public CollectionDownloadView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                
                // Uncomment this to enable the background image
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

            }
        );
    }
    
    
}

