using System.Collections.ObjectModel;
using Avalonia.Controls;
using NexusMods.App.UI.RightContent.DownloadGrid;
using NexusMods.App.UI.RightContent.LoadoutGrid;

namespace NexusMods.App.UI.Controls.DataGrid;

public static class Helpers
{
    public static IDisposable GenerateColumns<TColumnType>(this IObservable<ReadOnlyObservableCollection<IDataGridColumnFactory<TColumnType>>> factory, Avalonia.Controls.DataGrid target)
    where TColumnType : struct, Enum
    {
        return factory.OnUI()
            .SubscribeWithErrorLogging(default, c => GenerateColumnsImpl(target, c));
    }
    
    private static void GenerateColumnsImpl<TColumnType>(Avalonia.Controls.DataGrid target, ReadOnlyObservableCollection<IDataGridColumnFactory<TColumnType>> columns) 
        where TColumnType : struct, Enum
    {
        target.Columns.Clear();
        foreach (var column in columns)
        {
            var generatedColumn = column.Generate();
            generatedColumn.Header = GenerateHeader(column.Type);
            target.Columns.Add(generatedColumn);
        }
    }

    private static Control GenerateHeader<TColumnType>(this TColumnType column) where TColumnType : struct, Enum
    {
        return column switch
        {
            LoadoutColumn loadoutColumn => loadoutColumn switch
            {
                LoadoutColumn.Name => new TextBlock { Text = "NAME" },
                LoadoutColumn.Version => new TextBlock { Text = "VERSION" },
                LoadoutColumn.Category => new TextBlock { Text = "CATEGORY" },
                LoadoutColumn.Installed => new TextBlock { Text = "INSTALLED" },
                LoadoutColumn.Enabled => new RightContent.LoadoutGrid.Columns.ModEnabled.ModEnabledHeader()
            },
            DownloadColumn downloadColumn => downloadColumn switch
            {
                DownloadColumn.DownloadName => new TextBlock { Text = "MOD NAME" },
                DownloadColumn.DownloadVersion => new TextBlock { Text = "VERSION" },
                DownloadColumn.DownloadGameName => new TextBlock { Text = "GAME" },
                DownloadColumn.DownloadSize => new TextBlock { Text = "SIZE" },
                DownloadColumn.DownloadStatus => new TextBlock { Text = "STATUS" },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
        };
    }
}
