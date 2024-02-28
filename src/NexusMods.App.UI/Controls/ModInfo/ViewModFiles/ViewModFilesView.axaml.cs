using Avalonia.ReactiveUI;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers;
using ReactiveUI;

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
            ModFilesTreeDataGrid.Source = TreeDataGridHelpers.CreateTreeSource<ModFileNode, IFileTreeNodeViewModel, GamePath>(ViewModel!.Items);
        });
    }
}

