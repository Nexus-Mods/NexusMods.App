using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Humanizer;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public partial class CollectionLoadoutView : ReactiveUserControl<ICollectionLoadoutViewModel>
{
    public CollectionLoadoutView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionLoadoutView, ICollectionLoadoutViewModel, CompositeItemModel<EntityId>, EntityId>(this, TreeDataGrid, vm => vm.Adapter);

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Adapter.Source.Value, view => view.TreeDataGrid.Source)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.Name, view => view.CollectionName.Text)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.InstalledModsCount, view => view.NumDownloads.Text, v => $"{v:N0} Mods")
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.InstalledModsCount, view => view.RequiredDownloadsCount.Text, v => $"{v:N0}")
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.TileImage, view => view.CollectionImage.Source)
                .AddTo(disposables);
            
            this.WhenAnyValue(view => view.ViewModel!.TileImage)
                .Subscribe(image =>
                {
                    CollectionImageBorder.IsVisible = image != null;
                    CollectionImage.Source = image;
                })
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.EndorsementCount, view => view.Endorsements.Text,
                    v => Convert.ToInt32(v).ToMetric(null, 1)
                )
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.TotalDownloads, view => view.TotalDownloads.Text,
                    v => Convert.ToInt32(v).ToMetric(null, 1)
                )
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.TotalSize, view => view.TotalSize.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.OverallRating, view => view.OverallRating.Text, p => p.Value.ToString("P0"))
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.AuthorAvatar, view => view.AuthorAvatar.Source)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.RevisionNumber, view => view.Revision.Text, revision => $"Revision {revision}")
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                .AddTo(disposables);
            this.BindCommand(ViewModel, vm => vm.CommandToggle, view => view.CollectionToggle)
                .AddTo(disposables);
            this.BindCommand(ViewModel, vm => vm.CommandDeleteCollection, view => view.RemoveCollectionMenuItem)
                .AddTo(disposables);
            this.BindCommand(ViewModel, vm => vm.CommandMakeLocalEditableCopy, view => view.MakeEditableLocalCopy)
                .AddTo(disposables);
            this.BindCommand(ViewModel, vm => vm.CommandViewCollectionDownloadPage, view => view.ViewCollectionDownloadMenuItem)
                .AddTo(disposables);


            
            this.OneWayBind(ViewModel, vm => vm.IsLocalCollection, view => view.NexusModsLogo.IsVisible, static b => !b)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.IsReadOnly, view => view.ReadOnlyPillStack.IsVisible)
                .AddTo(disposables);

            this.WhenAnyValue(view => view.ViewModel!.IsCollectionEnabled)
                .WhereNotNull()
                .SubscribeWithErrorLogging(value =>
                    {
                        CollectionToggle.IsChecked = value;
                    }
                )
                .DisposeWith(disposables);
            
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
                        OverallRating.Text = className == "NoRating" ? "--" : ViewModel!.OverallRating.Value.ToString("P0");
                    }
                )
                .DisposeWith(disposables);
            
            this.WhenAnyValue(view => view.ViewModel!.BackgroundImage)
                .WhereNotNull()
                .SubscribeWithErrorLogging(image => HeaderBorderBackground.Background = new ImageBrush { Source = image, Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Top})
                .DisposeWith(disposables);
        });
    }
}
