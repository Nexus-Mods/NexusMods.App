using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI.Fody.Helpers;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutItemModel : TreeDataGridItemModel<LoadoutItemModel, EntityId>
{
    [Reactive] public DateTime InstalledAt { get; set; } = DateTime.UnixEpoch;

    public IObservable<string> NameObservable { get; init; } = System.Reactive.Linq.Observable.Return("-");
    [Reactive] public string Name { get; set; } = "-";

    public IObservable<string> VersionObservable { get; init; } = System.Reactive.Linq.Observable.Return("-");
    [Reactive] public string Version { get; set; } = "-";

    public IObservable<Size> SizeObservable { get; init; } = System.Reactive.Linq.Observable.Return(Size.Zero);
    [Reactive] public Size Size { get; set; } = Size.Zero;

    public IObservable<bool> IsEnabledObservable { get; init; } = System.Reactive.Linq.Observable.Return(false);
    public BindableReactiveProperty<bool> IsEnabled { get; } = new(value: false);

    public ReactiveCommand<Unit, IReadOnlyCollection<LoadoutItemId>> ToggleEnableStateCommand { get; }

    private readonly LoadoutItemId[] _fixedId;
    public virtual IReadOnlyCollection<LoadoutItemId> GetLoadoutItemIds() => _fixedId;

    private readonly IDisposable _modelActivationDisposable;
    public LoadoutItemModel(LoadoutItemId loadoutItemId)
    {
        _fixedId = [loadoutItemId];
        ToggleEnableStateCommand = new ReactiveCommand<Unit, IReadOnlyCollection<LoadoutItemId>>(_ => GetLoadoutItemIds());

        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.NameObservable.OnUI().Subscribe(name => model.Name = name).AddTo(disposables);
            model.VersionObservable.OnUI().Subscribe(version => model.Version = version).AddTo(disposables);
            model.SizeObservable.OnUI().Subscribe(size => model.Size = size).AddTo(disposables);
            model.IsEnabledObservable.OnUI().Subscribe(isEnabled => model.IsEnabled.Value = isEnabled).AddTo(disposables);
        });
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(_modelActivationDisposable, ToggleEnableStateCommand);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => Name;

    public static IColumn<LoadoutItemModel> CreateNameColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, string>(
            header: "NAME",
            getter: model => model.Name,
            options: new TextColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase),
                CompareDescending = static (a, b) => string.Compare(b?.Name, a?.Name, StringComparison.OrdinalIgnoreCase),
                IsTextSearchEnabled = true,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            },
            width: new GridLength(1, GridUnitType.Auto)
        )
        {
            SortDirection = ListSortDirection.Ascending,
            Id = "name",
        };
    }

    public static IColumn<LoadoutItemModel> CreateVersionColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, string>(
            header: "VERSION",
            getter: model => model.Version,
            options: new TextColumnOptions<LoadoutItemModel>
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

    public static IColumn<LoadoutItemModel> CreateSizeColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, Size>(
            header: "SIZE",
            getter: model => model.Size,
            options: new TextColumnOptions<LoadoutItemModel>
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

    public static IColumn<LoadoutItemModel> CreateInstalledAtColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, DateTime>(
            header: "INSTALLED",
            // TODO: use formatted date
            getter: model => model.InstalledAt,
            options: new TextColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => a?.InstalledAt.CompareTo(b?.InstalledAt ?? DateTime.UnixEpoch) ?? 1,
                CompareDescending = static (a, b) => b?.InstalledAt.CompareTo(a?.InstalledAt ?? DateTime.UnixEpoch) ?? 1,
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
                CompareAscending = static (a, b) => a?.IsEnabled.Value.CompareTo(b?.IsEnabled.Value ?? false) ?? 1,
                CompareDescending = static (a, b) => b?.IsEnabled.Value.CompareTo(a?.IsEnabled.Value ?? false) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Id = "Toggle"
        };
    }
}
