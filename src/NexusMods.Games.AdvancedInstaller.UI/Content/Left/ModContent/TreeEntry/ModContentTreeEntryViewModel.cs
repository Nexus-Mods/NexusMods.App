using System.Reactive;
using DynamicData.Kernel;

using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

public class ModContentTreeEntryViewModel : AViewModel<IModContentTreeEntryViewModel>, IModContentTreeEntryViewModel
{
    public RelativePath RelativePath { get; }
    public string FileName { get; }
    public bool IsDirectory { get; }
    public bool IsRoot { get; }
    public RelativePath Parent { get; }
    public bool IsTopLevelChild { get; }
    public string MappingFolderName { get; set; }
    public Optional<GamePath> Mapping { get; set; }
    [Reactive] public ModContentTreeEntryStatus Status { get; set; }
    public ReactiveCommand<Unit, Unit> BeginSelectCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelSelectCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }
    [Reactive] public bool IsExpanded { get; set; }

    /// <summary>
    /// Constructs a ModContent tree entry view model.
    /// </summary>
    /// <param name="relativePath">The path relative to the archive root identifying this entry.</param>
    /// <param name="isDirectory">Whether the path represents a directory or not.</param>
    public ModContentTreeEntryViewModel(
        RelativePath relativePath,
        bool isDirectory)
    {
        RelativePath = relativePath;
        IsDirectory = isDirectory;
        FileName = relativePath.FileName;
        IsRoot = RelativePath == RelativePath.Empty;

        // Use invalid parent path for root node, to avoid matching another node by accident.
        Parent = IsRoot ? IModContentTreeEntryViewModel.RootParentRelativePath : RelativePath.Parent;

        IsTopLevelChild = Parent == RelativePath.Empty;
        MappingFolderName = string.Empty;
        Status = ModContentTreeEntryStatus.Default;

        BeginSelectCommand = ReactiveCommand.Create(() => { });
        CancelSelectCommand = ReactiveCommand.Create(() => { });
        RemoveMappingCommand = ReactiveCommand.Create(() => { });
    }

    public void SetFileMapping(IPreviewTreeEntryViewModel entry, string mappingFolderName, bool isExplicit)
    {
        MappingFolderName = mappingFolderName;
        Mapping = entry.GamePath;
        Status = isExplicit ? ModContentTreeEntryStatus.IncludedExplicit : ModContentTreeEntryStatus.IncludedViaParent;
    }

    public void RemoveMapping()
    {
        MappingFolderName = string.Empty;
        Mapping = Optional.None<GamePath>();
        Status = ModContentTreeEntryStatus.Default;
    }
}
