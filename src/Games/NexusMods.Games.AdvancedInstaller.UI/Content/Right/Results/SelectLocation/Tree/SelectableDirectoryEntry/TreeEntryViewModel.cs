using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

public class TreeEntryViewModel : AViewModel<ITreeEntryViewModel>, ITreeEntryViewModel
{
    [Reactive]
    public SelectableDirectoryNodeStatus Status { get; internal set; } = SelectableDirectoryNodeStatus.Regular;

    public ObservableCollection<ITreeEntryViewModel> Children { get; init; } = new();

    public IAdvancedInstallerCoordinator Coordinator { get; }
    public ReactiveCommand<Unit, Unit> LinkCommand { get; private set; } = Initializers.EnabledReactiveCommand;
    public GamePath Path { get; init; }

    // Used for the "Create new folder" node.
    public static GamePath EmptyPath = new GamePath(LocationId.Unknown, string.Empty);

    public TreeEntryViewModel? Parent { get; init; }

    public string DirectoryName
    {
        get
        {
            if (Path == EmptyPath)
                return string.Empty;
            if (Path.FileName == string.Empty)
                return Path.LocationId.Value;
            return Path.FileName;
        }
    }

    private string _displayName = string.Empty;

    public TreeEntryViewModel(IAdvancedInstallerCoordinator coordinator)
    {
        Coordinator = coordinator;

        this.WhenActivated(disposables =>
        {
            LinkCommand = ReactiveCommand.Create(Link).DisposeWith(disposables);
        });
    }

    public string DisplayName => _displayName != string.Empty ? _displayName : DirectoryName;

    public void Link()
    {
        Coordinator.DirectorySelectedObserver.OnNext(this);
    }


    /// <summary>
    ///     Creates nodes from a given path that is tied to a FileSystem.
    /// </summary>
    /// <param name="absPath">Path of where <see cref="GamePath"/> points to on FileSystem.</param>
    /// <param name="gamePath">The path of the root node.</param>
    /// <param name="rootName">Name of the root item.</param>
    public static TreeEntryViewModel
        Create(AbsolutePath absPath, GamePath gamePath, IAdvancedInstallerCoordinator coordinator,  string rootName = "")
    {
        var finalLocation = absPath.Combine(gamePath.Path);
        var finalLocationLength = finalLocation.GetFullPathLength() + 1;
        var root = new TreeEntryViewModel(coordinator)
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = gamePath,
            _displayName = rootName,
        };
        root.CreateChildrenRecursive(finalLocation, gamePath.LocationId, finalLocationLength, coordinator);
        return root;
    }

    /// <summary>
    ///     Creates nodes from a given list of files.
    /// </summary>
    /// <param name="currentDirectory">The path to the current directory.</param>
    /// <param name="locationId">The named location for the <see cref="GamePath"/>(s) to create.</param>
    /// <param name="dirSubstringLength">Precalculated length of <see cref="currentDirectory"/>.</param>
    internal void CreateChildrenRecursive(AbsolutePath currentDirectory, LocationId locationId, int dirSubstringLength, IAdvancedInstallerCoordinator coordinator)
    {
        // Add the Create New Folder node.
        var createFolderNode = new TreeEntryViewModel(coordinator)
        {
            Status = SelectableDirectoryNodeStatus.Create,
            Path = EmptyPath,
            Parent = this,
        };
        Children.Add(createFolderNode);

        // Get files at this level.
        foreach (var directory in currentDirectory.EnumerateDirectories("*", false))
        {
            var name = directory.GetFullPath().Substring(dirSubstringLength);
            var node = new TreeEntryViewModel(coordinator)
            {
                Path = new GamePath(locationId, name),
                Parent = this,
            };
            node.CreateChildrenRecursive(directory, locationId, dirSubstringLength, coordinator);
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
