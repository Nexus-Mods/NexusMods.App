using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerModContentDesignViewModel : AdvancedInstallerModContentViewModel
{
    public static readonly TreeDataGridFileNode TestTree = new()
    {
        FileName = "All mod files", IsDirectory = true, IsRoot = true,
        Children = new ObservableCollection<TreeDataGridFileNode>()
        {
            new() { FileName = "BWS.bsa" },
            new() { FileName = "BWS - Textures.bsa" },
            new() { FileName = "Readme-BWS.txt" },
            new()
            {
                FileName = "Textures", IsDirectory = true,
                Children = new ObservableCollection<TreeDataGridFileNode>()
                {
                    new() { FileName = "greenBlade.dds" },
                    new() { FileName = "greenBlade_n.dds" },
                    new() { FileName = "greenHilt.dds" },
                    new()
                    {
                        FileName = "Armors", IsDirectory = true,
                        Children = new ObservableCollection<TreeDataGridFileNode>()
                        {
                            new() { FileName = "greenArmor.dds" },
                            new() { FileName = "greenBlade.dds" },
                            new() { FileName = "greenHilt.dds" },
                        },
                    },
                },
            },
            new()
            {
                FileName = "Meshes", IsDirectory = true,
                Children = new ObservableCollection<TreeDataGridFileNode>()
                {
                    new() { FileName = "greenBlade.nif" },
                }
            }
        }
    };

    public override HierarchicalTreeDataGridSource<TreeDataGridFileNode> Tree => new(TestTree)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<TreeDataGridFileNode>(
                new TemplateColumn<TreeDataGridFileNode>(null,
                    new FuncDataTemplate<TreeDataGridFileNode>((node, scope) => new AdvancedInstallerTreeEntryView()
                    {
                        DataContext = node,
                    }),
                    width: new GridLength(1, GridUnitType.Star)
                    ),
                x => x.Children)
        }
    };
}


