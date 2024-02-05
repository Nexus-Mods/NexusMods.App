using System.Collections.ObjectModel;
using Avalonia.Controls;
using NexusMods.App.UI.Controls.DownloadGrid;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.Resources;

namespace NexusMods.App.UI.Controls.DataGrid;

public static class Helpers
{
    public static IDisposable GenerateColumns<TColumnType>(
        this IObservable<ReadOnlyObservableCollection<IDataGridColumnFactory<TColumnType>>> factory,
        Avalonia.Controls.DataGrid target)
        where TColumnType : struct, Enum
    {
        return factory.OnUI()
            .SubscribeWithErrorLogging(default, c => GenerateColumnsImpl(target, c));
    }

    private static void GenerateColumnsImpl<TColumnType>(Avalonia.Controls.DataGrid target,
        ReadOnlyObservableCollection<IDataGridColumnFactory<TColumnType>> columns)
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
                LoadoutColumn.Name => new TextBlock { Text = Language.Helpers_GenerateHeader_NAME },
                LoadoutColumn.Version => new TextBlock { Text = Language.Helpers_GenerateHeader_VERSION },
                LoadoutColumn.Category => new TextBlock { Text = Language.Helpers_GenerateHeader_CATEGORY },
                LoadoutColumn.Installed => new TextBlock { Text = Language.Helpers_GenerateHeader_INSTALLED },
                LoadoutColumn.Enabled => new ModEnabledHeader()
            },
            DownloadColumn downloadColumn => downloadColumn switch
            {
                DownloadColumn.DownloadName => new TextBlock { Text = Language.Helpers_GenerateHeader_MOD_NAME },
                DownloadColumn.DownloadVersion => new TextBlock { Text = Language.Helpers_GenerateHeader_VERSION },
                DownloadColumn.DownloadGameName => new TextBlock { Text = Language.Helpers_GenerateHeader_GAME },
                DownloadColumn.DownloadSize => new TextBlock { Text = Language.Helpers_GenerateHeader_SIZE },
                DownloadColumn.DownloadStatus => new TextBlock { Text = Language.Helpers_GenerateHeader_STATUS },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
        };
    }
}
