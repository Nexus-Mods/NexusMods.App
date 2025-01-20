using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NuGet.Versioning;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

/// <summary>
///     When in the library, and using tree view, this shows the top level item that represents the top page.
/// </summary>
public class NexusModsModPageLibraryItemModel : TreeDataGridItemModel<ILibraryItemModel, EntityId>,
    ILibraryItemWithThumbnailAndName,
    ILibraryItemWithSize,
    ILibraryItemWithDates,
    ILibraryItemWithUpdateAction,
    ILibraryItemWithVersion, // Inherited from child, per design request
    IHasLinkedLoadoutItems,
    IIsParentLibraryItemModel
{
    public required IObservable<int> NumInstalledObservable { get; init; }
    private ObservableHashSet<NexusModsLibraryItem.ReadOnly> _libraryItems = [];

    public NexusModsModPageLibraryItemModel(
        IObservable<IChangeSet<NexusModsLibraryItem.ReadOnly, EntityId>> libraryItemsObservable, IObservable<bool> hasChildrenObservable,
        IObservable<IChangeSet<ILibraryItemModel, EntityId>> childrenObservable, IServiceProvider serviceProvider)
    {
        FormattedSize = ItemSize.ToFormattedProperty();
        FormattedDownloadedDate = DownloadedDate.ToFormattedProperty();
        FormattedInstalledDate = InstalledDate.ToFormattedProperty();
        HasChildrenObservable = hasChildrenObservable;
        ChildrenObservable = childrenObservable;
        InstallItemCommand = ILibraryItemWithInstallAction.CreateCommand(this);
        UpdateItemCommand = ILibraryItemWithUpdateAction.CreateCommand(this);

        // ReSharper disable once NotDisposedResource
        var datesDisposable = ILibraryItemWithDates.SetupDates(this);
        var modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);

        // NOTE(erri120): This subscription needs to be set up in the constructor and kept alive
        // until the entire model gets disposed. Without this, selection would break for off-screen items.
        var libraryItemsDisposable = libraryItemsObservable.OnUI()
            .Select(changeset =>
                {
                    _libraryItems.ApplyChanges(changeset);
                    return _libraryItems.FirstOrDefault();
                }
            )
            .Where(item => item.IsValid())
            .SelectMany(async item =>
            {
                // Skip mods without thumbnails
                if (!item.ModPageMetadata.Contains(NexusModsModPageMetadata.ThumbnailUri))
                    return item;
                
                // Note(sewer): Update the thumbnail of the header to be the thumbnail of the first child item.
                // By definition, all the sub items belong to this page, so the thumbnail of the child item
                // is the thumbnail of this page.
                //
                // SAFETY: This can't have race condition, code is executed on UI, so can't be executed in parallel.
                var thumbNail = await modPageThumbnailPipeline.LoadResourceAsync(item.ModPageMetadataId, CancellationToken.None);
                Thumbnail.Value = thumbNail.Data;
                return item;
            }).SubscribeWithErrorLogging();

        var linkedLoadoutItemsDisposable = new SerialDisposable();

        // ReSharper disable once NotDisposedResource
        var modelActivationDisposable = this.WhenActivated(linkedLoadoutItemsDisposable, static (self, linkedLoadoutItemsDisposable, disposables) =>
        {
            // ReSharper disable once NotDisposedResource
            IHasLinkedLoadoutItems.SetupLinkedLoadoutItems(self, linkedLoadoutItemsDisposable).AddTo(disposables);

            self._libraryItems
                .ObserveCountChanged(notifyCurrentCount: true)
                .Subscribe(self, static (count, self) =>
                {
                    if (count > 0)
                    {
                        self.DownloadedDate.Value = self._libraryItems.Max(static item => item.GetCreatedAt());
                        self.ItemSize.Value = self._libraryItems.Sum(static item => item.AsLibraryItem().TryGetAsLibraryFile(out var libraryFile) ? libraryFile.Size : Size.Zero);
                    }
                    else
                    {
                        self.DownloadedDate.Value = DateTimeOffset.UnixEpoch;
                        self.ItemSize.Value = Size.Zero;
                    }
                }).AddTo(disposables);

            self.NumInstalledObservable
                .ToObservable()
                .CombineLatest(
                    source2: self._libraryItems.ObserveCountChanged(notifyCurrentCount: true),
                    source3: ReactiveUI.WhenAnyMixin.WhenAnyValue(self, static self => self.IsExpanded).ToObservable(),
                    source4: self.IsInstalled,
                    static (numInstalled,numTotal,isExpanded , _) => (numInstalled, numTotal, isExpanded)
                )
                .ObserveOnUIThreadDispatcher()
                .Subscribe(self, static (tuple, self) =>
                {
                    var (numInstalled, numTotal, isExpanded) = tuple;
                    self.InstallButtonText.Value = ILibraryItemWithInstallAction.GetButtonText(numInstalled, numTotal, isExpanded);
                })
                .AddTo(disposables);
        });
        
        var setVersionDisposable = ChildrenObservable
            .ToCollection()
            .Subscribe(items =>
            {
                // N20250116
                // Note(sewer): Design says put highest version of child here.
                //
                // There is no 'standard' for version fields, as some sources
                // like the Nexus website allow you to specify anything in the version field.
                // We do a 'best effort' here by trying to parse as SemVer and using
                // that. There are some possible alternatives, e.g. 'by upload date',
                // however; that then requires additional logic, to not pick up other
                // mods on the same mod page, etc. So we go for something simple for now.
                var maxVersion = items
                    .OfType<ILibraryItemWithVersion>()
                    .Max(x => NuGetVersion.TryParse(x.Version.Value, out var version) 
                        ? version : new NuGetVersion(0, 0, 0));

                if (maxVersion != null)
                {
                    // SAFETY: Not null because >= 1 item.
                    _preUpdateVersion = maxVersion!.ToString();
                    Version.Value = _preUpdateVersion;
                } 
            });

        _modelDisposable = Disposable.Combine(
            datesDisposable,
            modelActivationDisposable,
            libraryItemsDisposable,
            setVersionDisposable,
            Name,
            ItemSize,
            FormattedSize,
            DownloadedDate,
            FormattedDownloadedDate,
            InstalledDate,
            FormattedInstalledDate,
            InstallItemCommand,
            IsInstalled,
            InstallButtonText
        );
    }

    public IReadOnlyList<LibraryItemId> LibraryItemIds => _libraryItems.Select(static x => (LibraryItemId)x.Id).ToArray();
    public NexusModsLibraryItem.ReadOnly[] LibraryItems => _libraryItems.ToArray();
    
    public Observable<DateTimeOffset>? Ticker { get; set; }

    public required IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>> LinkedLoadoutItemsObservable { get; init; }
    public ObservableDictionary<EntityId, LibraryLinkedLoadoutItem.ReadOnly> LinkedLoadoutItems { get; private set; } = [];

    public BindableReactiveProperty<Bitmap> Thumbnail { get; } = new();
    public BindableReactiveProperty<bool> ShowThumbnail { get; } = new(value: true);
    public BindableReactiveProperty<string> Name { get; } = new(value: "-");
    public BindableReactiveProperty<string> Version { get; } = new(value: "-");
    private string _preUpdateVersion = string.Empty;
    
    public ReactiveProperty<Size> ItemSize { get; } = new();
    public BindableReactiveProperty<string> FormattedSize { get; }

    public ReactiveProperty<DateTimeOffset> DownloadedDate { get; } = new();
    public BindableReactiveProperty<string> FormattedDownloadedDate { get; }

    public ReactiveProperty<DateTimeOffset> InstalledDate { get; } = new();
    public BindableReactiveProperty<string> FormattedInstalledDate { get; }

    public ReactiveCommand<Unit, ILibraryItemModel> InstallItemCommand { get; }
    public BindableReactiveProperty<bool> IsInstalled { get; } = new();
    public BindableReactiveProperty<string> InstallButtonText { get; } = new(value: ILibraryItemWithInstallAction.GetButtonText(isInstalled: false));

    public ReactiveCommand<Unit, ILibraryItemModel> UpdateItemCommand { get; }
    public BindableReactiveProperty<bool> UpdateAvailable { get; } = new(value: false);
    public BindableReactiveProperty<string> UpdateButtonText { get; } = new(value: ILibraryItemWithUpdateAction.GetButtonText(1, 1));
    
    private bool _isDisposed;
    private readonly IDisposable _modelDisposable;

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _modelDisposable.Dispose();
            }

            LinkedLoadoutItems = null!;
            _libraryItems = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => $"Nexus Mods Mod Page: {Name.Value}";

    /// <summary>
    /// Informs the mod page model of an available update to the item.
    /// </summary>
    /// <param name="mostRecentFile">The most recent file of any of the page's children.</param>
    /// <param name="filesToUpdate">
    ///     All files that are newer than a currently downloaded item.
    ///     This allows updating of all items belonging to this mod page.
    /// </param>
    public void InformAvailableUpdate(NexusModsFileMetadata.ReadOnly mostRecentFile, List<(NexusModsLibraryItem.ReadOnly oldItem, NexusModsFileMetadata.ReadOnly newItem)> filesToUpdate)
    {
        // Note(sewer): filesToUpdate is currently unused, will be used in future code.
        Version.Value = LibraryItemModelCommon.FormatModVersionUpdate(_preUpdateVersion, mostRecentFile.Version);
        UpdateAvailable.Value = true;
    }
}
