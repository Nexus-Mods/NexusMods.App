using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerModContentViewModel : AViewModel<IAdvancedInstallerModContentViewModel>,
    IAdvancedInstallerModContentViewModel
{
    public virtual HierarchicalTreeDataGridSource<TreeDataGridFileNode> Tree =>
        new(AdvancedInstallerModContentDesignViewModel.TestTree)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<TreeDataGridFileNode>(
                    new TemplateColumn<TreeDataGridFileNode>(
                        null,
                        new FuncDataTemplate<TreeDataGridFileNode>((node, scope) => new AdvancedInstallerTreeEntryView()
                        {
                            DataContext = node,
                        }),
                        width: new GridLength(1, GridUnitType.Star)
                    ),
                    x => x.Children
                )
            }
        };
}
