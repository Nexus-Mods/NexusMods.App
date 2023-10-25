using System.Collections.ObjectModel;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

public class TreeEntryViewModel : ReactiveObject, ITreeEntryViewModel
{
    [Reactive]
    public SelectableDirectoryNodeStatus Status { get; internal set; } = SelectableDirectoryNodeStatus.Regular;

    public ObservableCollection<ITreeEntryViewModel> Children { get; init; } = new();
    public GamePath Path { get; init; }
    public string DirectoryName => String.IsNullOrEmpty(Path.FileName) ? Path.LocationId.Value : Path.FileName;

    private string _displayName = string.Empty;
    public string DisplayName => _displayName != string.Empty ? _displayName : DirectoryName;

    /// <summary>
    ///     Creates nodes from a given path that is tied to a FileSystem.
    /// </summary>
    /// <param name="absPath">Path of where <see cref="GamePath"/> points to on FileSystem.</param>
    /// <param name="gamePath">The path of the root node.</param>
    /// <param name="rootName">Name of the root item.</param>
    public static TreeEntryViewModel Create(AbsolutePath absPath, GamePath gamePath, string rootName = "")
    {
        var finalLocation = absPath.Combine(gamePath.Path);
        var finalLocationLength = finalLocation.GetFullPathLength() + 1;
        var root = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = gamePath,
            _displayName = rootName
        };
        root.CreateChildrenRecursive(finalLocation, gamePath.LocationId, finalLocationLength);
        return root;
    }

    /// <summary>
    ///     Creates nodes from a given list of files.
    /// </summary>
    /// <param name="currentDirectory">The path to the current directory.</param>
    /// <param name="locationId">The named location for the <see cref="GamePath"/>(s) to create.</param>
    /// <param name="dirSubstringLength">Precalculated length of <see cref="currentDirectory"/>.</param>
    internal void CreateChildrenRecursive(AbsolutePath currentDirectory, LocationId locationId, int dirSubstringLength)
    {
        // Get files at this level.
        foreach (var directory in currentDirectory.EnumerateDirectories("*", false))
        {
            var name = directory.GetFullPath().Substring(dirSubstringLength);
            var node = new TreeEntryViewModel { Path = new GamePath(locationId, name) };
            node.CreateChildrenRecursive(directory, locationId, dirSubstringLength + name.Length + 1);
            Children.Add(node);
        }
    }
}

/// <summary>
///     Represents the current status of the <see cref="TreeEntryViewModel" />.
/// </summary>
public enum SelectableDirectoryNodeStatus
{
    /// <summary>
    /// Regular selectable directory node.
    /// </summary>
    Regular,

    /// <summary>
    /// Special "Create new folder" entry node.
    /// </summary>
    Create,

    /// <summary>
    /// Create node after button was pressed, user can input the name of the new folder.
    /// </summary>
    Editing,

    /// <summary>
    /// A new node created with "Create new folder" button
    /// </summary>
    Created,
}
