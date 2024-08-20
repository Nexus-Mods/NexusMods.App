using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using NexusMods.App.UI.Controls;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutItemModel : TreeDataGridItemModel<LoadoutItemModel>
{
    public string Name { get; set; } = "Unknown";

    public string Version { get; set; } = "-";

    public Size Size { get; set; } = Size.Zero;

    public required DateTime InstalledAt { get; init; }

    public bool IsEnabled { get; set; }
    public R3.ReactiveCommand<R3.Unit> ToggleEnableStateCommand { get; }

    public LoadoutItemModel()
    {
        ToggleEnableStateCommand = new R3.ReactiveCommand<R3.Unit>(_ => { /* TODO */ });
    }

    public override string ToString() => Name;

    public static IColumn<LoadoutItemModel> CreateNameColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, string>(
            header: "Name",
            getter: model => model.Name,
            options: new TextColumnOptions<LoadoutItemModel>
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

        public static IColumn<LoadoutItemModel> CreateVersionColumn()
    {
        return new CustomTextColumn<LoadoutItemModel, string>(
            header: "Version",
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
            header: "Size",
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
            header: "Installed",
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
        return new TemplateColumn<LoadoutItemModel>(
            header: "Toggle",
            cellTemplateResourceKey: "ToggleEnableColumnTemplate",
            options: new TemplateColumnOptions<LoadoutItemModel>
            {
                CompareAscending = static (a, b) => a?.IsEnabled.CompareTo(b?.IsEnabled ?? false) ?? 1,
                CompareDescending = static (a, b) => b?.IsEnabled.CompareTo(a?.IsEnabled ?? false) ?? 1,
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        );
    }
}
