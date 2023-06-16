using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public static class Helpers
{
    public static IDisposable GenerateColumns(this IObservable<ReadOnlyObservableCollection<IDataGridColumnFactory>> factory, DataGrid target)
    {
        return factory.OnUI()
            .SubscribeWithErrorLogging(default, c => GenerateColumnsImpl(target, c));
    }
    
    private static void GenerateColumnsImpl(DataGrid target, ReadOnlyObservableCollection<IDataGridColumnFactory> columns)
    {
        target.Columns.Clear();
        foreach (var column in columns)
        {
            var generatedColumn = column.Generate();
            generatedColumn.Header = column.Type.GenerateHeader();
            target.Columns.Add(generatedColumn);
        }
    }

    private static Control GenerateHeader(this ColumnType type)
    {
        return type switch
        {
            ColumnType.Name => new TextBlock { Text = "NAME" },
            ColumnType.Version => new TextBlock { Text = "VERSION" },
            ColumnType.Category => new TextBlock { Text = "CATEGORY" },
            ColumnType.Installed => new TextBlock { Text = "INSTALLED" },
            ColumnType.Enabled => new ModEnabled.ModEnabledHeader(),
            ColumnType.DownloadName => new TextBlock { Text = "MOD NAME" },
            ColumnType.DownloadVersion => new TextBlock { Text = "VERSION" },
            ColumnType.DownloadGameName => new TextBlock { Text = "GAME" },
            ColumnType.DownloadSize => new TextBlock { Text = "SIZE" },
            ColumnType.DownloadStatus => new TextBlock { Text = "STATUS" },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
