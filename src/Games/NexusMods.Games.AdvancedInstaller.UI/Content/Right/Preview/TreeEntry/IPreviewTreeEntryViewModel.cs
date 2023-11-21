using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData.Kernel;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

/// <summary>
///     Represents an individual tree node in the Preview section.
///     It represents a single file or directory that will be installed, creating the final folder structure of the mod.
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

    public Optional<IModContentTreeEntryViewModel> MappedEntry { get; set; }

    public ObservableCollection<IModContentTreeEntryViewModel> MappedEntries { get; }

    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    public void AddMapping(IModContentTreeEntryViewModel entry);

    public void RemoveFileMapping();

    public void RemoveDirectoryMapping(IModContentTreeEntryViewModel entry);

    public static GamePath RootParentGamePath = new(LocationId.Unknown, "");
}
