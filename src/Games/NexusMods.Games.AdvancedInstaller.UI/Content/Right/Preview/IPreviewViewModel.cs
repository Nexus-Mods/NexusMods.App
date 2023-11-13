using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

public interface IPreviewViewModel : IViewModelInterface
{
    /// <summary>
    /// The TreeContainers to display in the preview.
    /// Each of the ContainerVM corresponds to a different <see cref="LocationId"/>.
    /// </summary>
    public ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> TreeContainers { get; }

    public SourceCache<IPreviewTreeEntryViewModel, GamePath> TreeEntriesCache { get; }
    public ReadOnlyObservableCollection<TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>> TreeRoots { get; }

}

// /// <summary>
// ///     Extension methods for <see cref="IPreviewViewModel"/>.
// /// </summary>
// public static class PreviewViewModelExtensions
// {
//     /// <summary>
//     ///     Gets or creates a location inside the given <see cref="IPreviewViewModel"/>.
//     /// </summary>
//     /// <param name="vm">The ViewModel where the locations are stored.</param>
//     /// <param name="isDirectory"></param>
//     /// <param name="directoryPath">The path of the item in question.</param>
//     /// <param name="sourceItemPath">The filename of the link source item (mod content entry)</param>
//     public static IModContentBindingTarget GetOrCreateBindingTarget(this IPreviewViewModel vm,
//         RelativePath sourceItemPath, bool isDirectory, GamePath directoryPath)
//     {
//         // When binding the archive root (empty path), don't append filename or it will create an additional folder for the root.
//         var targetPath = sourceItemPath == RelativePath.Empty
//             ? directoryPath.Path
//             : directoryPath.Path.Join(sourceItemPath.FileName);
//
//         var location = vm.TreeContainers.FirstOrDefault(l => l.Root.Item.GamePath.LocationId == directoryPath.LocationId);
//         if (location is not null)
//             return location.Root.GetOrCreateChild(targetPath, isDirectory);
//
//         location = new LocationPreviewTreeViewModel(new GamePath(directoryPath.LocationId, RelativePath.Empty));
//         vm.ContainersCache.AddOrUpdate(location);
//         return location.Root.GetOrCreateChild(targetPath, isDirectory);
//     }
// }
