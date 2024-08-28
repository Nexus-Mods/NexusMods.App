using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryItemModel : TreeDataGridItemModel<LibraryItemModel>
{
    [Reactive] public DynamicData.Kernel.Optional<LibraryItemId> LibraryItemId { get; set; }

    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    [Reactive] public Size Size { get; set; } = Size.Zero;
    [Reactive] public string Version { get; set; } = "-";

    public IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly>> LinkedLoadoutItemsObservable { get; init; } = System.Reactive.Linq.Observable.Empty<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly>>();
    private ObservableCollectionExtended<LibraryLinkedLoadoutItem.ReadOnly> LinkedLoadoutItems { get; set; } = [];

    [Reactive] public bool IsInstalledInLoadout { get; set; }
    [Reactive] public DateTime InstalledDate { get; set; } = DateTime.UnixEpoch;

    public Observable<DateTime>? Ticker { get; set; }
    [Reactive] public string FormattedCreatedAtDate { get; set; } = new("-");
    [Reactive] public string FormattedInstalledDate { get; set; } = new("-");

    public R3.ReactiveCommand<Unit, LibraryItemId> InstallCommand { get; }

    private readonly IDisposable _modelActivationDisposable;
    public LibraryItemModel()
    {
        var canInstall = this.WhenAnyValue(
            static model => model.IsInstalledInLoadout,
            static model => model.LibraryItemId,
            static (isInstalled, libraryItemId) => !isInstalled && libraryItemId.HasValue
        ).ToObservable();

        InstallCommand = new R3.ReactiveCommand<Unit, LibraryItemId>(canExecuteSource: canInstall, initialCanExecute: false, convert: _ => LibraryItemId.Value);

        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.FormattedCreatedAtDate = FormatDate(DateTime.Now, model.CreatedAt);
            model.FormattedInstalledDate = FormatDate(DateTime.Now, model.InstalledDate);

            Debug.Assert(model.Ticker is not null, "should've been set before activation");
            model.Ticker.Subscribe(model, static (now, model) =>
            {
                model.FormattedCreatedAtDate = FormatDate(now, model.CreatedAt);
                model.FormattedInstalledDate = FormatDate(now, model.InstalledDate);
            }).AddTo(disposables);

            model.LinkedLoadoutItems
                .ObserveCollectionChanges()
                .SubscribeWithErrorLogging(_ =>
                {
                    if (model.LinkedLoadoutItems.Count > 0)
                    {
                        model.IsInstalledInLoadout = true;
                        model.InstalledDate = model.LinkedLoadoutItems.Select(static item => item.GetCreatedAt()).Max();
                        model.FormattedInstalledDate = FormatDate(DateTime.Now, model.InstalledDate);
                    }
                    else
                    {
                        model.IsInstalledInLoadout = false;
                        model.InstalledDate = DateTime.UnixEpoch;
                    }
                }).AddTo(disposables);

            model.LinkedLoadoutItemsObservable.OnUI().Bind(model.LinkedLoadoutItems).SubscribeWithErrorLogging().AddTo(disposables);
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
                Disposable.Dispose(InstallCommand, _modelActivationDisposable);
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
            getter: model => model.Version,
            options: new TextColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => string.Compare(a?.Version, b?.Version, StringComparison.OrdinalIgnoreCase),
                CompareDescending = static (a, b) => string.Compare(b?.Version, a?.Version, StringComparison.OrdinalIgnoreCase),
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
            getter: model => model.Size,
            options: new TextColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => a is null ? -1 : a.Size.CompareTo(b?.Size ?? Size.Zero),
                CompareDescending = static (a, b) => b is null ? -1 : b.Size.CompareTo(a?.Size ?? Size.Zero),
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
            getter: model => model.FormattedCreatedAtDate,
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
            getter: model => model.FormattedInstalledDate,
            options: new TextColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => a?.InstalledDate.CompareTo(b?.InstalledDate ?? DateTime.UnixEpoch) ?? 1,
                CompareDescending = static (a, b) => b?.InstalledDate.CompareTo(a?.InstalledDate ?? DateTime.UnixEpoch) ?? 1,
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
        return new TemplateColumn<LibraryItemModel>(
            header: "ACTIONS",
            cellTemplateResourceKey: "InstallColumnTemplate",
            options: new TemplateColumnOptions<LibraryItemModel>
            {
                CompareAscending = static (a, b) => a?.IsInstalledInLoadout.CompareTo(b?.IsInstalledInLoadout ?? false) ?? 1,
                CompareDescending = static (a, b) => b?.IsInstalledInLoadout.CompareTo(a?.IsInstalledInLoadout ?? false) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        );
    }
}
