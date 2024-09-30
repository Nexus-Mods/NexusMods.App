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
                
                this.WhenAnyValue(view => view.ViewModel!.Name)
                    .BindTo(this, view => view.TitleText.Text)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.Image)
                    .BindTo(this, view => view.TileImage.Source)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.Category)
                    .BindTo(this, view => view.CategoryText.Text)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.Summary)
                    .BindTo(this, view => view.SummaryText.Text)
                    .DisposeWith(d);
                this.WhenAnyValue(view => view.ViewModel!.ModCount)
                    .BindTo(this, view => view.ModCount.Text)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.EndorsementCount)
                    .BindTo(this, view => view.Endorsements.Text)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.DownloadCount)
                    .BindTo(this, view => view.Downloads.Text)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.TotalSize)
                    .BindTo(this, view => view.TotalSize.Text)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.OverallRating)
                    .BindTo(this, view => view.OverallRating.Text)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.AuthorName)
                    .BindTo(this, view => view.AuthorName.Text)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.AuthorAvatar)
                    .BindTo(this, view => view.AuthorAvatarImage.Source)
                    .DisposeWith(d);
                
                this.BindCommand(ViewModel, vm => vm.ShowDetailsCommand, view => view.DetailsButton)
                    .DisposeWith(d);
            }
        );
    }
}

