using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    public ITreeDataGridSource<LoadoutNode> Source { get; }

    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var nodes = GetNodesFlat(connection, loadoutId);

        Source = CreateSource(nodes, createHierarchicalSource: true);
    }

    private IEnumerable<LoadoutNode> GetNodesFlat(IConnection connection, LoadoutId loadoutId)
    {
        var db = connection.Db;

        var libraryLinkedLoadoutItems = db.Datoms(LibraryLinkedLoadoutItem.LibraryItem).AsModels<LibraryLinkedLoadoutItem.ReadOnly>(db);
        foreach (var entity in libraryLinkedLoadoutItems)
        {
            var loadoutItem = entity.AsLoadoutItem();

            if (loadoutItem.LoadoutId != loadoutId) continue;
            if (!loadoutItem.TryGetAsLoadoutItemGroup(out var group)) continue;

            foreach (var child in group.Children)
            {
                if (!child.IsLoadoutItemGroup()) continue;

                yield return new LoadoutNode
                {
                    Name = $"{entity.LibraryItem.Name} => {child.Name}",
                };
            }
        }
    }

    private static ITreeDataGridSource<LoadoutNode> CreateSource(IEnumerable<LoadoutNode> nodes, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<LoadoutNode>(nodes);
            AddColumns(source.Columns, viewAsTree: true);
            return source;
        }
        else
        {
            var source = new FlatTreeDataGridSource<LoadoutNode>(nodes);
            AddColumns(source.Columns, viewAsTree: false);
            return source;
        }
    }

    private static void AddColumns(ColumnList<LoadoutNode> columnList, bool viewAsTree)
    {
        var nameColumn = LoadoutNode.CreateNameColumn();
        columnList.Add(viewAsTree ? LoadoutNode.CreateExpanderColumn(nameColumn) : nameColumn);
    }
}
