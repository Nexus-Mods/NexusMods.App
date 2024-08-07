using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using DynamicData;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.App.UI.Resources;
using NexusMods.Paths;

namespace NexusMods.App.UI.Controls.Trees;

public class LoadoutGroupFileTreeViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    public ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
    public ReadOnlyObservableCollection<string> StatusBarStrings { get; }

    public LoadoutGroupFileTreeViewModel(LoadoutItemGroup.ReadOnly group)
    {
        var totalNumFiles = 0;
        var totalSize = Size.Zero;

        var nodes = new List<IFileTreeNodeViewModel>();
        var directories = new Dictionary<GamePath, (Size size, uint numFiles, GamePath path, GamePath parentPath)>();

        foreach (var loadoutItem in group.Children.OfTypeLoadoutItemWithTargetPath())
        {
            var size = Size.Zero;
            if (loadoutItem.TryGetAsLoadoutFile(out var loadoutFile))
            {
                size = loadoutFile.Size;
            }

            var node = new FileTreeNodeViewModel(
                fullPath: loadoutItem.TargetPath,
                parentPath: ((GamePath)loadoutItem.TargetPath).Parent,
                isFile: true,
                fileSize: size.Value,
                numChildFiles: 0,
                isDeletion: loadoutItem.IsDeletedFile()
            );

            nodes.Add(node);

            totalNumFiles++;
            totalSize += size;

            var parent = ((GamePath)loadoutItem.TargetPath).Parent;
            while (parent.Path != RelativePath.Empty)
            {
                ref var directory = ref CollectionsMarshal.GetValueRefOrNullRef(directories, parent);
                var hasDirectory = !Unsafe.IsNullRef(ref directory);
                if (!hasDirectory)
                {
                    directories.Add(parent, (size, numFiles: 1, path: parent, parentPath: parent.Parent));
                }
                else
                {
                    directory.size += size;
                    directory.numFiles += 1;
                }

                parent = parent.Parent;
            }

            ref var rootDirectory = ref CollectionsMarshal.GetValueRefOrNullRef(directories, parent);
            var hasRootDirectory = !Unsafe.IsNullRef(ref rootDirectory);
            if (!hasRootDirectory)
            {
                directories.Add(parent, (size, numFiles: 1, path: parent,parentPath: IFileTreeViewModel.RootParentGamePath));
            }
            else
            {
                rootDirectory.size += size;
                rootDirectory.numFiles += 1;
            }
        }

        var locationsRegister = group.AsLoadoutItem().Loadout.InstallationInstance.LocationsRegister;
        foreach (var kv in directories)
        {
            var (_, directory) = kv;
            var (size, numFiles, path, parentPath) = directory;

            if (parentPath.Equals(IFileTreeViewModel.RootParentGamePath))
            {
                nodes.Add(new FileTreeNodeViewModel(
                    name: locationsRegister[path.LocationId].ToString(),
                    fullPath: path,
                    parentPath: parentPath,
                    isFile: false,
                    fileSize: size.Value,
                    numChildFiles: numFiles
                ));
            }
            else
            {
                nodes.Add(new FileTreeNodeViewModel(
                    fullPath: path,
                    parentPath: parentPath,
                    isFile: false,
                    fileSize: size.Value,
                    numChildFiles: numFiles,
                    isDeletion: false
                ));
            }
        }

        var sourceCache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.Key);
        sourceCache.AddOrUpdate(nodes);
        sourceCache
            .Connect()
            .TransformToTree(vm => vm.ParentKey)
            .Transform(node => node.Item.Initialize(node))
            .Bind(out var roots)
            .Subscribe();

        TreeSource = CreateTreeSource(roots);
        TreeSource.SortBy(TreeSource.Columns[0], ListSortDirection.Ascending);

        StatusBarStrings = new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(new[]
        {
            string.Format(Language.ModFileTreeViewModel_StatusBar_Files__0__1, totalNumFiles, ByteSize.FromBytes(totalSize.Value).ToString()),
        }));
    }

    internal static HierarchicalTreeDataGridSource<IFileTreeNodeViewModel> CreateTreeSource(
        ReadOnlyObservableCollection<IFileTreeNodeViewModel> roots)
    {
        return new HierarchicalTreeDataGridSource<IFileTreeNodeViewModel>(roots)
        {
            Columns =
            {
                FileTreeNodeViewModel.CreateTreeSourceNameColumn(),
                FileTreeNodeViewModel.CreateTreeSourceSizeColumn(),
                FileTreeNodeViewModel.CreateTreeSourceFileCountColumn(),
            }
        };
    }
}
