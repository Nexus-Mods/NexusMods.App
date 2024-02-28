using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using Splat;

namespace NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using ModFileNode = TreeNodeVM<IFileTreeNodeViewModel, GamePath>;

public partial class ViewModFilesView : ReactiveUserControl<IViewModFilesViewModel>
{
    public ViewModFilesView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            // Unleash the tree!
            ModFilesTreeDataGrid.Source = CreateTreeSource(ViewModel!.Items);
        });
    }
    
    private static HierarchicalTreeDataGridSource<ModFileNode> CreateTreeSource(
        ReadOnlyObservableCollection<ModFileNode> treeRoots)
    {
        var locator = Locator.Current.GetService<IViewLocator>();
        return new HierarchicalTreeDataGridSource<ModFileNode>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<ModFileNode>(
                    new TemplateColumn<ModFileNode>(null,
                        new FuncDataTemplate<ModFileNode>((node, _) =>
                            {
                                // This should never be null but can be during rapid resize, due to
                                // virtualization shenanigans. Think this is a control bug, but well, gotta work with what we have.
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                                if (node == null)
                                    return new Control();
                                    
                                // Very sus but it works, t r u s t.
                                var view = locator!.ResolveView(node.Item);
                                var ctrl = view as Control;
                                ctrl!.DataContext = node.Item;
                                return ctrl;
                            }
                        ),
                        width: new GridLength(1, GridUnitType.Star)
                    ),
                    node => node.Children,
                    null,  
                    node => node.IsExpanded),
                
                new TextColumn<ModFileNode,long?>(
                    Language.Helpers_GenerateHeader_SIZE,
                    x => 9999
                    /*
                    ,
                    options: new()
                    {
                        CompareAscending = IViewModFilesViewModel.SortAscending(x => x.Size),
                        CompareDescending = IViewModFilesViewModel.SortDescending(x => x.Size),
                    }
                    */
                ),
            }
        };
    }
}

