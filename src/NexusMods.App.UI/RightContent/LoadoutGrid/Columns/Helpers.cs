using Avalonia.Controls;
using Avalonia.Layout;
using Projektanker.Icons.Avalonia;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public static class Helpers
{
    public static Control GenerateHeader(this ColumnType type)
    {
        return type switch
        {
            ColumnType.Name => new TextBlock { Text = "NAME" },
            ColumnType.Version => new TextBlock { Text = "VERSION" },
            ColumnType.Category => new TextBlock { Text = "CATEGORY" },
            ColumnType.Installed => new TextBlock { Text = "INSTALLED" },
            ColumnType.Enabled => new ModEnabledHeader()
        };
    }
}
