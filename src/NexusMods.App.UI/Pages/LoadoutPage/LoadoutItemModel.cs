using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

[Obsolete("Use CompositeItemModel instead")]
public class LoadoutItemModel : TreeDataGridItemModel<LoadoutItemModel, EntityId>
{
    public ReactiveProperty<DateTimeOffset> InstalledAt { get; } = new(DateTime.UnixEpoch);

    public BindableReactiveProperty<Bitmap> Thumbnail { get; } = new();
    
    public BindableReactiveProperty<bool> ShowThumbnail { get; } = new(value: false);
    
    public IObservable<string> NameObservable { get; init; } = System.Reactive.Linq.Observable.Return("-");
    public BindableReactiveProperty<string> Name { get; } = new("-");
    

    public IObservable<string> VersionObservable { get; init; } = System.Reactive.Linq.Observable.Return("-");
    public BindableReactiveProperty<string> Version { get; } = new("-");

    public IObservable<Size> SizeObservable { get; init; } = System.Reactive.Linq.Observable.Return(Size.Zero);
    public BindableReactiveProperty<Size> ItemSize { get; } = new(Size.Zero);

    public IObservable<bool?> IsEnabledObservable { get; init; } = System.Reactive.Linq.Observable.Return<bool?>(false);
    public BindableReactiveProperty<bool?> IsEnabled { get; } = new(value: false);

    public ReactiveCommand<Unit, IReadOnlyCollection<(LoadoutItemId Id, bool ShouldEnable)>> ToggleEnableStateCommand { get; }

    private readonly LoadoutItemId[] _fixedId;
    public virtual IReadOnlyCollection<LoadoutItemId> GetLoadoutItemIds() => _fixedId;

    public Observable<DateTime>? Ticker { get; set; }
    public BindableReactiveProperty<string> FormattedInstalledAt { get; } = new("-");

    private readonly IDisposable _modelActivationDisposable;
    public LoadoutItemModel(LoadoutItemId loadoutItemId, IServiceProvider serviceProvider, IConnection connection, bool loadThumbnail, bool showThumbnail)
    {
        _fixedId = [loadoutItemId];
        ToggleEnableStateCommand = new ReactiveCommand<Unit, IReadOnlyCollection<(LoadoutItemId Id, bool ShouldEnable)>>(_ =>
        {
            var ids = GetLoadoutItemIds();
            var isEnabled = IsEnabled.Value;
            var shouldEnable = !isEnabled ?? false;

            return ids.Select(id => (Id: id, ShouldEnable: shouldEnable)).ToArray();
        });

        var state = (loadoutItemId, serviceProvider, connection, loadThumbnail, showThumbnail);
        _modelActivationDisposable = this.WhenActivated(state, static (model, tuple, disposables) =>
        {
            var (loadoutItemId, serviceProvider, connection, loadThumbnail, showThumbnail) = tuple;
            
            model.ShowThumbnail.Value = showThumbnail;
            if (loadThumbnail)
            {
                var modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
                var libraryLinkedItem = LibraryLinkedLoadoutItem.Load(connection.Db, loadoutItemId);
                if (libraryLinkedItem.IsValid() && libraryLinkedItem.LibraryItem.TryGetAsNexusModsLibraryItem(out var nexusLibraryItem))
                {
                    ImagePipelines.CreateObservable(nexusLibraryItem.ModPageMetadataId, modPageThumbnailPipeline)
                        .ObserveOnUIThreadDispatcher()
                        .Subscribe(model, static (bitmap, self) => self.Thumbnail.Value = bitmap)
                        .AddTo(disposables);
                }
            }
            
            Debug.Assert(model.Ticker is not null, "should've been set before activation");
            model.Ticker.Subscribe(model, static (now, model) =>
            {
                model.FormattedInstalledAt.Value = FormatDate(now, model.InstalledAt.Value);
            }).AddTo(disposables);

            model.NameObservable.OnUI().Subscribe(name => model.Name.Value = name).AddTo(disposables);
            model.VersionObservable.OnUI().Subscribe(version => model.Version.Value = version).AddTo(disposables);
            model.SizeObservable.OnUI().Subscribe(size => model.ItemSize.Value = size).AddTo(disposables);
            model.IsEnabledObservable.OnUI().Subscribe(isEnabled => model.IsEnabled.Value = isEnabled).AddTo(disposables);

            model.InstalledAt.Subscribe(model, static (date, model) => model.FormattedInstalledAt.Value = FormatDate(DateTime.Now, date)).AddTo(disposables);
        });
    }

    private static string FormatDate(DateTimeOffset now, DateTimeOffset date)
    {
        if (date == DateTimeOffset.UnixEpoch || date == default(DateTimeOffset)) return "-";
        return date.Humanize(dateToCompareAgainst: now > date ? now : TimeProvider.System.GetLocalNow());
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(
                    _modelActivationDisposable,
                    ToggleEnableStateCommand,
                    InstalledAt,
                    FormattedInstalledAt,
                    Name,
                    Version,
                    ItemSize,
                    IsEnabled
                );
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => Name.Value;

    public static IColumn<LoadoutItemModel> CreateThumbnailAndNameColumn()
    {
        return new CustomTemplateColumn<LoadoutItemModel>(
            header: "NAME",
            cellTemplateResourceKey: "DisplayThumbnailAndNameColumnTemplate",
            options: new TemplateColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => string.Compare(a?.Name.Value, b?.Name.Value, StringComparison.OrdinalIgnoreCase),
                CompareDescending = static (a, b) => string.Compare(b?.Name.Value, a?.Name.Value, StringComparison.OrdinalIgnoreCase),
                IsTextSearchEnabled = true,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            SortDirection = ListSortDirection.Ascending,
            Id = "LibraryItemNameColumn",
        };
    }

    public static IColumn<LoadoutItemModel> CreateVersionColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, string>(
            header: "VERSION",
            getter: model => model.Version.Value,
            options: new TextColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => string.Compare(a?.Version.Value, b?.Version.Value, StringComparison.OrdinalIgnoreCase),
                CompareDescending = static (a, b) => string.Compare(b?.Version.Value, a?.Version.Value, StringComparison.OrdinalIgnoreCase),
                IsTextSearchEnabled = true,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "version",
        };
    }

    public static IColumn<LoadoutItemModel> CreateSizeColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, Size>(
            header: "SIZE",
            getter: model => model.ItemSize.Value,
            options: new TextColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => a is null ? -1 : a.ItemSize.Value.CompareTo(b?.ItemSize.Value ?? Size.Zero),
                CompareDescending = static (a, b) => b is null ? -1 : b.ItemSize.Value.CompareTo(a?.ItemSize.Value ?? Size.Zero),
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "size",
        };
    }

    public static IColumn<LoadoutItemModel> CreateInstalledAtColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, string>(
            header: "INSTALLED",
            getter: model => model.FormattedInstalledAt.Value,
            options: new TextColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => a?.InstalledAt.Value.CompareTo(b?.InstalledAt.Value ?? DateTime.UnixEpoch) ?? 1,
                CompareDescending = static (a, b) => b?.InstalledAt.Value.CompareTo(a?.InstalledAt.Value ?? DateTime.UnixEpoch) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "InstalledAt",
        };
    }

    public static IColumn<LoadoutItemModel> CreateToggleEnableColumn()
    {
        return new CustomTemplateColumn<LoadoutItemModel>(
            header: "TOGGLE",
            cellTemplateResourceKey: "ToggleEnableColumnTemplate",
            options: new TemplateColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => a?.IsEnabled.Value?.CompareTo(b?.IsEnabled.Value ?? false) ?? 1,
                CompareDescending = static (a, b) => b?.IsEnabled.Value?.CompareTo(a?.IsEnabled.Value ?? false) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "Toggle",
        };
    }
}
