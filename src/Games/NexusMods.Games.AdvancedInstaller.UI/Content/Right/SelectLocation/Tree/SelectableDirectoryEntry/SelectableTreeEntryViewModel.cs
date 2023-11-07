using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SelectableTreeEntryViewModel : AViewModel<ISelectableTreeEntryViewModel>, ISelectableTreeEntryViewModel
{
    [Reactive]
    public SelectableDirectoryNodeStatus Status { get; internal set; } = SelectableDirectoryNodeStatus.Regular;

    [Reactive] public string InputText { get; set; } = string.Empty;

    [Reactive] private bool CanSave { get; set; }

    public ObservableCollection<ISelectableTreeEntryViewModel> Children { get; init; } = new();

    public IAdvancedInstallerCoordinator Coordinator { get; }
    public ReactiveCommand<Unit, Unit> LinkCommand { get; }

    public ReactiveCommand<Unit, Unit> EditCreateFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCreatedFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCreateFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCreatedFolderCommand { get; }
    public GamePath Path { get; init; }

    // Used for the "Create new folder" node.
    private static readonly GamePath EmptyPath = new GamePath(LocationId.Unknown, string.Empty);

    public SelectableTreeEntryViewModel? Parent { get; init; }

    private static readonly string InvalidFolderCharsRegex =
        "[" + String.Concat(System.IO.Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/' })) + "]";

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

    public SelectableTreeEntryViewModel(IAdvancedInstallerCoordinator coordinator)
    {
        Coordinator = coordinator;
        LinkCommand = ReactiveCommand.Create(Link);
        EditCreateFolderCommand = ReactiveCommand.Create(OnEditCreateFolder);
        SaveCreatedFolderCommand = ReactiveCommand.Create(OnSaveCreatedFolder, this.WhenAnyValue(x => x.CanSave));
        CancelCreateFolderCommand = ReactiveCommand.Create(OnCancelCreatedFolder);
        DeleteCreatedFolderCommand = ReactiveCommand.Create(OnDeleteCreatedFolder);

        this.WhenActivated(disposables =>
        {
            // Update CanSave, checking if the input text isn't empty after removing invalid characters.
            this.WhenAnyValue(x => x.InputText)
                .Subscribe(text =>
                {
                    if (text == string.Empty)
                    {
                        CanSave = false;
                    }
                    else
                    {
                        var trimmed = RemoveInvalidFolderCharacter(text);
                        CanSave = trimmed != string.Empty;
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private void OnDeleteCreatedFolder()
    {
        Parent!.Children.Remove(this);
    }

    private void OnCancelCreatedFolder()
    {
        Status = SelectableDirectoryNodeStatus.Create;
        InputText = string.Empty;
    }

    private void OnSaveCreatedFolder()
    {
        var folderName = RelativePath.FromUnsanitizedInput(RemoveInvalidFolderCharacter(InputText));
        if (folderName == RelativePath.Empty)
            return;

        // Create a new child node in the parent with the given name.
        var newNode = new SelectableTreeEntryViewModel(Coordinator)
        {
            Status = SelectableDirectoryNodeStatus.Created,
            Path = new GamePath(Parent!.Path.LocationId, Parent!.Path.Path.Join(folderName)),
            Parent = Parent,
        };
        // Add a new CreateFolder node under it.
        newNode.Children.Add(new SelectableTreeEntryViewModel(Coordinator)
        {
            Status = SelectableDirectoryNodeStatus.Create,
            Path = EmptyPath,
            Parent = newNode,
        });
        // Add the new node to the parent.
        Parent.Children.Add(newNode);

        // Reset this to Create state.
        Status = SelectableDirectoryNodeStatus.Create;
        InputText = string.Empty;
    }

    private string RemoveInvalidFolderCharacter(string name)
    {
        var trimmed = name.Trim();
        if (trimmed == string.Empty)
            return trimmed;
        trimmed = Regex.Replace(trimmed, InvalidFolderCharsRegex, "");
        return trimmed;
    }

    private void OnEditCreateFolder()
    {
        InputText = string.Empty;
        Status = SelectableDirectoryNodeStatus.Editing;
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
    /// <param name="coordinator">BodyVM containing observables to notify on</param>
    /// <param name="rootName">Name of the root item.</param>
    public static SelectableTreeEntryViewModel
        Create(AbsolutePath absPath, GamePath gamePath, IAdvancedInstallerCoordinator coordinator, string rootName = "")
    {
        var finalLocation = absPath.Combine(gamePath.Path);
        var finalLocationLength = finalLocation.GetFullPathLength() + 1;
        var root = new SelectableTreeEntryViewModel(coordinator)
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
    /// <param name="coordinator">BodyVM containing observables to notify on</param>
    private void CreateChildrenRecursive(AbsolutePath currentDirectory, LocationId locationId, int dirSubstringLength,
        IAdvancedInstallerCoordinator coordinator)
    {
        // Add the Create New Folder node.
        var createFolderNode = new SelectableTreeEntryViewModel(coordinator)
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
            var node = new SelectableTreeEntryViewModel(coordinator)
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
///     Represents the current status of the <see cref="SelectableTreeEntryViewModel" />.
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
