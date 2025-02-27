using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Resources;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public partial class CollectionDownloadView : ReactiveUserControl<ICollectionDownloadViewModel>
{
    public CollectionDownloadView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionDownloadView, ICollectionDownloadViewModel, CompositeItemModel<EntityId>, EntityId>(this, DownloadsTree, vm => vm.TreeDataGridAdapter);

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.CommandViewOnNexusMods, view => view.MenuItemViewOnNexusMods)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CommandOpenJsonFile, view => view.MenuItemOpenJsonFile)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CommandDeleteCollectionRevision, view => view.MenuItemDeleteCollectionRevision)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.CollectionStatusText, view => view.TextCollectionStatus.Text)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CommandViewCollection, view => view.ButtonViewCollection)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CommandDownloadRequiredItems, view => view.ButtonDownloadRequiredItems)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.CommandInstallRequiredItems, view => view.ButtonInstallRequiredItems)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CommandDownloadOptionalItems, view => view.ButtonDownloadOptionalItems)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.CommandInstallOptionalItems, view => view.ButtonInstallOptionalItems)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CommandUpdateCollection, view => view.ButtonUpdateCollection)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.TreeDataGridAdapter.Source.Value, view => view.DownloadsTree.Source)
                .DisposeWith(d);

             this.WhenAnyValue(view => view.ViewModel!.BackgroundImage)
                 .WhereNotNull()
                 .SubscribeWithErrorLogging(image => HeaderBorderBackground.Background = new ImageBrush { Source = image, Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Top})
                 .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.TileImage)
                .WhereNotNull()
                .SubscribeWithErrorLogging(image => CollectionImage.Source = image)
                .DisposeWith(d);

            this.WhenAnyValue(
                    view => view.ViewModel!.IsDownloading.Value,
                    view => view.ViewModel!.IsInstalling.Value,
                    static (isDownloading, isInstalling) => isDownloading || isInstalling
                )
                .Subscribe(isActive => Spinner.IsVisible = isActive);

            this.OneWayBind(ViewModel, vm => vm.Name, view => view.Heading.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AuthorAvatar, view => view.AuthorAvatar.Source)
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

            this.OneWayBind(ViewModel, vm => vm.RevisionNumber, view => view.Revision.Text, revision => $"Revision {revision}")
                .DisposeWith(d);

            this.WhenAnyValue(
                    view => view.ViewModel!.IsUpdateAvailable.Value,
                    view => view.ViewModel!.NewestRevisionNumber.Value)
                .Subscribe(tuple =>
                {
                    var (isUpdateAvailable, optional) = tuple;
            
                    ButtonUpdateCollection.IsVisible = isUpdateAvailable;
                    ArrowRight.IsVisible = isUpdateAvailable;
                    NewestRevision.IsVisible = isUpdateAvailable;
            
                    if (optional.HasValue)
                    {
                        ButtonUpdateCollection.Text = string.Format(Language.CollectionDownloadViewModel_UpdateCollection, optional.Value);
                        NewestRevision.Text = $"Revision {optional.Value}";
                    }
                }).DisposeWith(d);
            
            this.WhenAnyValue(view => view.TabControl.SelectedItem)
                .Subscribe(selectedItem =>
                {
                    CollectionDownloadsFilter filter;
            
                    if (ReferenceEquals(selectedItem, RequiredTab))
                    {
                        filter = CollectionDownloadsFilter.OnlyRequired;
                    } else if (ReferenceEquals(selectedItem, OptionalTab))
                    {
                        filter = CollectionDownloadsFilter.OnlyOptional;
                    } else
                    {
                        return;
                    }
            
                    ViewModel!.TreeDataGridAdapter.Filter.Value = filter;
                }).DisposeWith(d);
            
            this.WhenAnyValue(
                view => view.ViewModel!.CountDownloadedRequiredItems,
                view => view.ViewModel!.CountDownloadedOptionalItems,
                view => view.ViewModel!.IsInstalled.Value,
                view => view.ViewModel!.HasInstalledAllOptionalItems.Value)
                .CombineLatest(ViewModel!.TreeDataGridAdapter.Filter.AsSystemObservable(), (a, b) => (a.Item1, a.Item2, a.Item3, a.Item4, b))
                .Subscribe(tuple =>
                {
                    var (countDownloadedRequiredItems, countDownloadedOptionalItems, isInstalled, hasInstalledAllOptionals, filter) = tuple;
                    var hasDownloadedAllRequiredItems = countDownloadedRequiredItems == ViewModel!.RequiredDownloadsCount;
                    var hasDownloadedAllOptionalItems = countDownloadedOptionalItems == ViewModel!.OptionalDownloadsCount;
            
                    ButtonViewCollection.IsVisible = isInstalled;
            
                    ButtonDownloadRequiredItems.IsVisible = !hasDownloadedAllRequiredItems;
                    ButtonInstallRequiredItems.IsVisible = !isInstalled && hasDownloadedAllRequiredItems;
            
                    ButtonDownloadOptionalItems.IsVisible = filter == CollectionDownloadsFilter.OnlyOptional && !hasDownloadedAllOptionalItems;
                    ButtonInstallOptionalItems.IsVisible = filter == CollectionDownloadsFilter.OnlyOptional && hasDownloadedAllOptionalItems && !hasInstalledAllOptionals;
                }).DisposeWith(d);
            
            this.WhenAnyValue(
                    view => view.ViewModel!.OptionalDownloadsCount,
                    view => view.ViewModel!.InstructionsRenderer,
                    static (count, renderer) => count == 0 && renderer is null)
                .Subscribe(hasSingleTab =>
                {
                    if (hasSingleTab) TabControl.Classes.Add("SingleTab");
                    else TabControl.Classes.Remove("SingleTab");
            
                    if (hasSingleTab) TabControl.SelectedItem = RequiredTab;
                }).DisposeWith(d);

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

            this.WhenAnyValue(view => view.ViewModel!.CanDownloadAutomatically)
                .Subscribe(canDownloadAutomatically =>
                {
                    ButtonDownloadRequiredItems.LeftIcon = canDownloadAutomatically ? null : IconValues.Lock;
                    ButtonDownloadOptionalItems.LeftIcon = canDownloadAutomatically ? null : IconValues.Lock;
                }).DisposeWith(d);

            this.WhenAnyValue(
                    view => view.ViewModel!.InstructionsRenderer,
                    view => view.ViewModel!.RequiredModsInstructions,
                    view => view.ViewModel!.OptionalModsInstructions)
                .Subscribe(tuple =>
                {
                    var (instructionsRenderer, requiredModsInstructions, optionalModsInstructions) = tuple;

                    var hasInstructions = instructionsRenderer is not null || requiredModsInstructions.Length > 0 || optionalModsInstructions.Length > 0;
                    InstructionsTab.IsVisible = hasInstructions;

                    CollectionInstructionsExpander.IsVisible = instructionsRenderer is not null;
                    CollectionInstructionsRendererHost.ViewModel = instructionsRenderer;

                    RequiredModsInstructionsExpander.IsVisible = requiredModsInstructions.Length > 0;
                    RequiredModsInstructions.ItemsSource = requiredModsInstructions;

                    OptionalModsInstructionsExpander.IsVisible = optionalModsInstructions.Length > 0;
                    OptionalModsInstructions.ItemsSource = optionalModsInstructions;
                }).DisposeWith(d);
        });
    }
}

