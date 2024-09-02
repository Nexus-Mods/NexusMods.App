using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Humanizer;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryItemModel : TreeDataGridItemModel<LibraryItemModel, EntityId>
{
    public ReactiveProperty<DynamicData.Kernel.Optional<LibraryItemId>> LibraryItemId { get; } = new();

    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public BindableReactiveProperty<Size> ItemSize { get; } = new(Size.Zero);
    public BindableReactiveProperty<string> Version { get; set; } = new("-");

    public IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>> LinkedLoadoutItemsObservable { get; init; } = System.Reactive.Linq.Observable.Empty<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>>();
    private ObservableList<LibraryLinkedLoadoutItem.ReadOnly> LinkedLoadoutItems { get; set; } = [];

    public ReactiveProperty<bool> IsInstalledInLoadout { get; } = new(false);
    public ReactiveProperty<DateTime> InstalledDate { get; } = new(DateTime.UnixEpoch);

    public Observable<DateTime>? Ticker { get; set; }
    public BindableReactiveProperty<string> FormattedCreatedAtDate { get; } = new("-");
    public BindableReactiveProperty<string> FormattedInstalledDate { get; } = new("-");

    public ReactiveCommand<Unit, LibraryItemId> InstallCommand { get; }

    private readonly IDisposable _modelActivationDisposable;
    public LibraryItemModel()
    {
        var canInstall = IsInstalledInLoadout
            .Select(static isInstalled => isInstalled)
            .CombineLatest(
                source2: LibraryItemId.Select(static libraryItemId => libraryItemId.HasValue),
                resultSelector: static (isInstalled, hasLibraryItem) => !isInstalled && hasLibraryItem
            );

        InstallCommand = canInstall.ToReactiveCommand<Unit, LibraryItemId>(_ => LibraryItemId.Value.Value, initialCanExecute: false);

        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.FormattedCreatedAtDate.Value = FormatDate(DateTime.Now, model.CreatedAt);
            model.FormattedInstalledDate.Value = FormatDate(DateTime.Now, model.InstalledDate.Value);

            Debug.Assert(model.Ticker is not null, "should've been set before activation");
            model.Ticker.Subscribe(model, static (now, model) =>
            {
                model.FormattedCreatedAtDate.Value = FormatDate(now, model.CreatedAt);
                model.FormattedInstalledDate.Value = FormatDate(now, model.InstalledDate.Value);
            }).AddTo(disposables);

            model.LinkedLoadoutItems
                .ObserveCountChanged()
                .Subscribe(model, static (count, model) =>
                {
                    if (count > 0)
                    {
                        model.IsInstalledInLoadout.Value = true;
                        model.InstalledDate.Value = model.LinkedLoadoutItems.Select(static item => item.GetCreatedAt()).Max();
                        model.FormattedInstalledDate.Value = FormatDate(DateTime.Now, model.InstalledDate.Value);
                    }
                    else
                    {
                        model.IsInstalledInLoadout.Value = false;
                        model.InstalledDate.Value = DateTime.UnixEpoch;
                    }
                }).AddTo(disposables);

            model.LinkedLoadoutItemsObservable.OnUI().SubscribeWithErrorLogging(changeSet => model.LinkedLoadoutItems.ApplyChanges(changeSet)).AddTo(disposables);
            Disposable.Create(model.LinkedLoadoutItems, static items => items.Clear()).AddTo(disposables);
        });
    }

    private static string FormatDate(DateTime now, DateTime date)
    {
        if (date == DateTime.UnixEpoch || date == default(DateTime)) return "-";
        return date.Humanize(dateToCompareAgainst: now);
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(
                    InstallCommand,
                    _modelActivationDisposable,
                    FormattedCreatedAtDate,
                    FormattedInstalledDate,
                    ItemSize,
                    LibraryItemId,
                    IsInstalledInLoadout,
                    InstalledDate
                );
            }

            LinkedLoadoutItems = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => Name;

    public static IColumn<LibraryItemModel> CreateNameColumn()
    {
        return new CustomTextColumn<LibraryItemModel, string>(
            header: "NAME",
            getter: model => model.Name,
            options: new TextColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase),
                CompareDescending = static (a, b) => string.Compare(b?.Name, a?.Name, StringComparison.OrdinalIgnoreCase),
                IsTextSearchEnabled = true,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            SortDirection = ListSortDirection.Ascending,
            Id = "name",
        };
    }

    public static IColumn<LibraryItemModel> CreateVersionColumn()
    {
        return new CustomTextColumn<LibraryItemModel, string>(
            header: "VERSION",
            getter: model => model.Version.Value,
            options: new TextColumnOptions<LibraryItemModel>
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

    public static IColumn<LibraryItemModel> CreateSizeColumn()
    {
        return new CustomTextColumn<LibraryItemModel, Size>(
            header: "SIZE",
            getter: model => model.ItemSize.Value,
            options: new TextColumnOptions<LibraryItemModel>
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

    public static IColumn<LibraryItemModel> CreateAddedAtColumn()
    {
        return new CustomTextColumn<LibraryItemModel, string>(
            header: "ADDED",
            getter: model => model.FormattedCreatedAtDate.Value,
            options: new TextColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => a?.CreatedAt.CompareTo(b?.CreatedAt ?? DateTime.UnixEpoch) ?? 1,
                CompareDescending = static (a, b) => b?.CreatedAt.CompareTo(a?.CreatedAt ?? DateTime.UnixEpoch) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "AddedAt",
        };
    }

    public static IColumn<LibraryItemModel> CreateInstalledAtColumn()
    {
        return new CustomTextColumn<LibraryItemModel, string>(
            header: "INSTALLED",
            getter: model => model.FormattedInstalledDate.Value,
            options: new TextColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => a?.InstalledDate.Value.CompareTo(b?.InstalledDate.Value ?? DateTime.UnixEpoch) ?? 1,
                CompareDescending = static (a, b) => b?.InstalledDate.Value.CompareTo(a?.InstalledDate.Value ?? DateTime.UnixEpoch) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "InstalledAt",
        };
    }

    public static IColumn<LibraryItemModel> CreateInstallColumn()
    {
        return new CustomTemplateColumn<LibraryItemModel>(
            header: "ACTIONS",
            cellTemplateResourceKey: "InstallColumnTemplate",
            options: new TemplateColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => a?.IsInstalledInLoadout.Value.CompareTo(b?.IsInstalledInLoadout.Value ?? false) ?? 1,
                CompareDescending = static (a, b) => b?.IsInstalledInLoadout.Value.CompareTo(a?.IsInstalledInLoadout.Value ?? false) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "Install",
        };
    }
}
