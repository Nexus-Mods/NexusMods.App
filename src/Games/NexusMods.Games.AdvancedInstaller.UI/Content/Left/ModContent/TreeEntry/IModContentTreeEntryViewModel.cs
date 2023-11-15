using System.Reactive;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

public interface IModContentTreeEntryViewModel : IViewModelInterface
{
    public RelativePath RelativePath { get; }

    public string FileName { get; }

    public bool IsDirectory { get; }

    public string DisplayName { get; }

    public bool IsRoot { get; }

    public RelativePath Parent { get; }

    public bool IsTopLevelChild { get; }

    public GamePath? MappingParentPath { get; set; }

    public string MappingFolderName { get; set; }

    public GamePath? Mapping { get; set; }

    public ModContentTreeEntryStatus Status { get; set; }

    public ReactiveCommand<Unit, Unit> BeginSelectCommand { get; }

    ReactiveCommand<Unit, Unit> CancelSelectCommand { get; }

    ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    public void SetFileMapping(IPreviewTreeEntryViewModel entry, string mappingFolderName, bool isExplicit);

    public void RemoveMapping();
}

/// <summary>
///     Represents the current status of the <see cref="NexusMods.Games.AdvancedInstaller.UI.ModContent.IModContentTreeEntryViewModel" />.
/// </summary>
public enum ModContentTreeEntryStatus : byte
{
    /// <summary>
    ///     Item is not selected, and available for selection.
    /// </summary>
    Default,

    /// <summary>
    ///     The item target is currently being selected/mapped.
    ///     This is used by the item which is currently being mapped into an install location.
    /// </summary>
    Selecting,

    /// <summary>
    ///     A parent of this item (folder) is currently being selected/mapped.
    /// </summary>
    /// <remarks>
    ///     When this state is active, the UI shows 'include' for files, and 'include folder' for folders.
    /// </remarks>
    SelectingViaParent,

    /// <summary>
    ///     Item is included, with explicit target location.
    /// </summary>
    /// <remarks>
    ///     When this state is active, the UI usually shows the name of the linked folder in the associated button.
    /// </remarks>
    IncludedExplicit,

    /// <summary>
    ///     Item id included, because a parent (folder) of the item is included.
    ///     When the parent is unlinked, this node is also unlinked.
    /// </summary>
    /// <remarks>
    ///     This is used to indicate a parent of this item which which is a directory has status
    ///     <see cref="IncludedExplicit" />.
    /// </remarks>
    IncludedViaParent
}
