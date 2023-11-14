using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
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

    public bool IsFolderDupe { get; }

    public IModContentTreeEntryViewModel? MappedEntry { get; set; }

    public ObservableCollection<IModContentTreeEntryViewModel> MappedPaths { get; }

    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    public void AddFileMapping(IModContentTreeEntryViewModel entry);

    public void RemoveFileMapping();

    public static GamePath RootParentGamePath = new(LocationId.Unknown, "");


}
