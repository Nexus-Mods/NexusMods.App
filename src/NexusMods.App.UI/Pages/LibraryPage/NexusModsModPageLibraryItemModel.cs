using DynamicData;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class NexusModsModPageLibraryItemModel : TreeDataGridItemModel<ILibraryItemModel, EntityId>,
    ILibraryItemWithName,
    ILibraryItemWithSize,
    ILibraryItemWithDates,
    ILibraryItemWithInstallAction,
    IHasLinkedLoadoutItems,
    IIsParentLibraryItemModel
{
    public required IObservable<int> NumInstalledObservable { get; init; }
    private ObservableHashSet<NexusModsLibraryItem.ReadOnly> LibraryItems { get; set; } = [];

    public NexusModsModPageLibraryItemModel(IObservable<IChangeSet<NexusModsLibraryItem.ReadOnly, EntityId>> libraryItemsObservable)
    {
        FormattedSize = ItemSize.ToFormattedProperty();
        FormattedDownloadedDate = DownloadedDate.ToFormattedProperty();
        FormattedInstalledDate = InstalledDate.ToFormattedProperty();
        InstallItemCommand = ILibraryItemWithInstallAction.CreateCommand(this);

        // ReSharper disable once NotDisposedResource
        var datesDisposable = ILibraryItemWithDates.SetupDates(this);

        // NOTE(erri120): This subscription needs to be set up in the constructor and kept alive
        // until the entire model gets disposed. Without this, selection would break for off-screen items.
        var libraryItemsDisposable =  libraryItemsObservable.OnUI().SubscribeWithErrorLogging(changeSet => LibraryItems.ApplyChanges(changeSet));

        var linkedLoadoutItemsDisposable = new SerialDisposable();

        // ReSharper disable once NotDisposedResource
        var modelActivationDisposable = this.WhenActivated(linkedLoadoutItemsDisposable, static (self, linkedLoadoutItemsDisposable, disposables) =>
        {
            // ReSharper disable once NotDisposedResource
            IHasLinkedLoadoutItems.SetupLinkedLoadoutItems(self, linkedLoadoutItemsDisposable).AddTo(disposables);

            self.LibraryItems
                .ObserveCountChanged(notifyCurrentCount: true)
                .Subscribe(self, static (count, self) =>
                {
                    if (count > 0)
                    {
                        self.DownloadedDate.Value = self.LibraryItems.Max(static item => item.GetCreatedAt());
                        self.ItemSize.Value = self.LibraryItems.Sum(static item => item.AsLibraryItem().TryGetAsLibraryFile(out var libraryFile) ? libraryFile.Size : Size.Zero);
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
                    source2: self.LibraryItems.ObserveCountChanged(notifyCurrentCount: true),
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

        _modelDisposable = Disposable.Combine(
            datesDisposable,
            modelActivationDisposable,
            libraryItemsDisposable,
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

    public IReadOnlyList<LibraryItemId> LibraryItemIds => LibraryItems.Select(static x => (LibraryItemId)x.Id).ToArray();

    public Observable<DateTimeOffset>? Ticker { get; set; }

    public required IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>> LinkedLoadoutItemsObservable { get; init; }
    public ObservableDictionary<EntityId, LibraryLinkedLoadoutItem.ReadOnly> LinkedLoadoutItems { get; private set; } = [];

    public BindableReactiveProperty<string> Name { get; } = new(value: "-");

    public ReactiveProperty<Size> ItemSize { get; } = new();
    public BindableReactiveProperty<string> FormattedSize { get; }

    public ReactiveProperty<DateTimeOffset> DownloadedDate { get; } = new();
    public BindableReactiveProperty<string> FormattedDownloadedDate { get; }

    public ReactiveProperty<DateTimeOffset> InstalledDate { get; } = new();
    public BindableReactiveProperty<string> FormattedInstalledDate { get; }

    public ReactiveCommand<Unit, ILibraryItemModel> InstallItemCommand { get; }
    public BindableReactiveProperty<bool> IsInstalled { get; } = new();
    public BindableReactiveProperty<string> InstallButtonText { get; } = new(value: ILibraryItemWithInstallAction.GetButtonText(isInstalled: false));

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
            LibraryItems = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => $"Nexus Mods Mod Page: {Name.Value}";
}
