using System.Reactive.Disposables;
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
                this.WhenAnyValue(view => view.ViewModel!.TileImage)
                    .WhereNotNull()
                    .SubscribeWithErrorLogging(image => TileImage.Source = image)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.Name, view => view.Title.Text)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Summary, view => view.Summary.Text)
                    .DisposeWith(d);

            }
        );
    }
    
    
}

