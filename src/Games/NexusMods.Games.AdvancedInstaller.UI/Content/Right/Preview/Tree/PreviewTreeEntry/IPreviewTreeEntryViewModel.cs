using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
/// <remarks>
///     Trees are build from top level LocationIds, all descendants are relative to the root GamePath.
///     If it happens that after a deletion, no files are deployed, the entire tree should be cleared.
/// </remarks>
public interface IPreviewTreeEntryViewModel : IViewModelInterface
{
    public GamePath GamePath { get; }

    public GamePath Parent { get; }

    public string DisplayName { get; }

    public bool IsDirectory { get; }

    public bool IsRoot { get; }

    public bool IsRemovable { get; set; }

    public bool IsNew { get; }

    public bool IsFolderMerged { get; set; }

    public bool IsFolderDupe { get; set; }

    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    public static GamePath RootParentGamePath = new(LocationId.Unknown, "");
}
