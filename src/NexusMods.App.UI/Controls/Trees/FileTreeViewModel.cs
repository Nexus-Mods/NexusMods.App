using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Resources;

namespace NexusMods.App.UI.Controls.Trees;

public class FileTreeViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    private readonly SourceCache<IFileTreeNodeViewModel, GamePath> _sourceCache;
    private ReadOnlyObservableCollection<IFileTreeNodeViewModel> _items;
    
    public ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
    
    public FileTreeViewModel(ReadOnlyObservableCollection<IFileTreeNodeViewModel> treeRoots)
    {
        _items = new ReadOnlyObservableCollection<IFileTreeNodeViewModel>([]);
        _sourceCache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.Key);
        TreeSource = CreateTreeSource(treeRoots);
    }
    
    private static HierarchicalTreeDataGridSource<IFileTreeNodeViewModel> CreateTreeSource(
        ReadOnlyObservableCollection<IFileTreeNodeViewModel> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<IFileTreeNodeViewModel>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IFileTreeNodeViewModel>(
                    new TemplateColumn<IFileTreeNodeViewModel>(
                        Language.Helpers_GenerateHeader_NAME,
                        "CustomRow",
                        width: new GridLength(1, GridUnitType.Star),
                        options: new TemplateColumnOptions<IFileTreeNodeViewModel>
                        {
                            // Compares if folder first, such that folders show first, then by file name.
                            CompareAscending = (x, y) =>
                            {
                                if (x == null || y == null) return 0;
                                var folderComparison = x.IsFile.CompareTo(y.IsFile);
                                return folderComparison != 0 ? folderComparison : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                            },

                            CompareDescending = (x, y) =>
                            {
                                if (x == null || y == null) return 0;
                                var folderComparison = x.IsFile.CompareTo(y.IsFile);
                                return folderComparison != 0 ? folderComparison : string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase);
                            },
                        }
                    ),
                    node => node.Children,
                    null,
                    node => node.IsExpanded
                ),

                new TextColumn<IFileTreeNodeViewModel, string?>(
                    Language.Helpers_GenerateHeader_SIZE,
                    x => ByteSize.FromBytes(x.FileSize).ToString(),
                    options: new TextColumnOptions<IFileTreeNodeViewModel>
                    {
                        // Compares if folder first, such that folders show first, then by file name.
                        CompareAscending = (x, y) =>
                        {
                            if (x == null || y == null) return 0;
                            var folderComparison = x.IsFile.CompareTo(y.IsFile);
                            return folderComparison != 0 ? folderComparison : x.FileSize.CompareTo(y.FileSize);
                        },

                        CompareDescending = (x, y) =>
                        {
                            if (x == null || y == null) return 0;
                            var folderComparison = x.IsFile.CompareTo(y.IsFile);
                            return folderComparison != 0 ? folderComparison : y.FileSize.CompareTo(x.FileSize);
                        },
                    },
                    width: new GridLength(100)
                ),
            }
        };
    }
}
