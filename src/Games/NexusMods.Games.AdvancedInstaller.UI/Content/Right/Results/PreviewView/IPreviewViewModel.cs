using System.Collections.ObjectModel;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public interface IPreviewViewModel : IViewModel
{
    /// <summary>
    ///     The locations to display in the preview.
    ///     Each of the locations corresponds to a different <see cref="LocationId"/>.
    /// </summary>
    public ObservableCollection<ILocationPreviewTreeViewModel> Locations { get; }
}

/// <summary>
///     Extension methods for <see cref="IPreviewViewModel"/>.
/// </summary>
public static class PreviewViewModelExtensions
{
    /// <summary>
    ///     Gets or creates a location inside the given <see cref="IPreviewViewModel"/>.
    /// </summary>
    /// <param name="vm">The ViewModel where the locations are stored.</param>
    /// <param name="directoryPath">The path of the item in question.</param>
    public static IModContentBindingTarget GetOrCreateBindingTarget(this IPreviewViewModel vm, RelativePath sourceItemPath, bool isDirectory, GamePath directoryPath)
    {
        // When binding the archive root (empty path), don't append filename or it will create an additional folder for the root.
        var targetPath = sourceItemPath == RelativePath.Empty ? directoryPath.Path : directoryPath.Path.Join(sourceItemPath.FileName);

        var location = vm.Locations.FirstOrDefault(l => l.Root.FullPath.LocationId == directoryPath.LocationId);
        if (location is not null)
            return location.Root.GetOrCreateChild(targetPath, isDirectory);

        location = new LocationPreviewTreeViewModel(new GamePath(directoryPath.LocationId, RelativePath.Empty));
        vm.Locations.Add(location);
        return location.Root.GetOrCreateChild(targetPath, isDirectory);
    }
}
