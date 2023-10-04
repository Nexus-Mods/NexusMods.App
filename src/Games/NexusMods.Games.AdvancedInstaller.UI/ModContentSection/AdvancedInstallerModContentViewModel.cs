using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
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
                    new TemplateColumn<TreeDataGridFileNode>("File",
                        new FuncDataTemplate<TreeDataGridFileNode>((node, scope) => new AdvancedInstallerTreeEntryView()
                        {
                            DataContext = node,
                        })),
                    x => x.Children)
            }
        };
}
