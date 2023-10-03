using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerModContentViewModel : AViewModel<IAdvancedInstallerModContentViewModel>,
    IAdvancedInstallerModContentViewModel
{
    public virtual HierarchicalTreeDataGridSource<TreeDataGridFileNode> Tree =>
        new(new TreeDataGridFileNode())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<TreeDataGridFileNode>(
                    new TextColumn<TreeDataGridFileNode, string>("File Name", x => x.FileName), x => x.Children)
            }
        };
}
