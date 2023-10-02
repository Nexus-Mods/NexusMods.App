using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerModContentDesignViewModel : AdvancedInstallerModContentViewModel
{
    private readonly TreeDataGridFileNode _testTree = new()
    {
        FileName = "All mod files", IsDirectory = false, IsRoot = true,
    };

    public override HierarchicalTreeDataGridSource<TreeDataGridFileNode> Tree => new HierarchicalTreeDataGridSource<TreeDataGridFileNode>(_testTree)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<TreeDataGridFileNode>(
                new TextColumn<TreeDataGridFileNode, string>("File Name", x => x.FileName), x => x.Children)
        }
    };
}


